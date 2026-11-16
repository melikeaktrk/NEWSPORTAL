using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NewsPortal.Infrastructure.Persistence;

namespace NewsPortal.Infrastructure.Feeds;

public sealed record RssFeedSource(Guid Id, string Name, string FeedUrl, string Category, bool IsActive, DateTimeOffset? LastFetchedAt = null, string? LastError = null);
public sealed record RssNewsItem(Guid Id, Guid FeedId, string SourceName, string Title, string Summary, string OriginalUrl, string? ImageUrl, string Category, DateTimeOffset? OriginalPublishedAt, DateTimeOffset ImportedAt, string Status);

public sealed class RssNewsService(IHttpClientFactory clients, ILogger<RssNewsService> logger, IConfiguration configuration, IDbContextFactory<NewsPortalDbContext> dbFactory) : BackgroundService
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public async Task<IReadOnlyList<RssFeedSource>> GetSourcesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.RssFeedSources.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new RssFeedSource(x.Id, x.Name, x.FeedUrl, x.Category, x.IsActive, x.LastFetchedAt, x.LastError))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RssNewsItem>> GetItemsAsync(string? status = null, string? category = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.RssNewsItems.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (category is not null) query = query.Where(x => x.Category == category);
        return await query.OrderByDescending(x => x.OriginalPublishedAt ?? x.ImportedAt).Take(100)
            .Select(x => new RssNewsItem(x.Id, x.FeedId, x.SourceName, x.Title, x.Summary, x.OriginalUrl, x.ImageUrl, x.Category, x.OriginalPublishedAt, x.ImportedAt, x.Status))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<RssFeedSource> AddSourceAsync(string name, string url, string category, CancellationToken cancellationToken)
    {
        var uri = await ValidateUrlAsync(url, cancellationToken);
        var normalizedCategory = Slug(category);
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) throw new ArgumentException("Kaynak adı 1–100 karakter olmalıdır.");
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.RssFeedSources.AsNoTracking().FirstOrDefaultAsync(x => x.FeedUrl == uri.AbsoluteUri, cancellationToken);
        if (existing is not null) return ToModel(existing);
        var source = new RssFeedSource(Guid.NewGuid(), name.Trim(), uri.AbsoluteUri, normalizedCategory, true);
        db.RssFeedSources.Add(ToEntity(source));
        await db.SaveChangesAsync(cancellationToken);
        return source;
    }

    public async Task<int> RefreshAllAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var total = 0;
            foreach (var feed in (await GetSourcesAsync(cancellationToken)).Where(x => x.IsActive))
            {
                try { total += await RefreshFeedAsync(feed, cancellationToken); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "RSS kaynağı alınamadı: {FeedName}", feed.Name);
                    await UpdateFeedStatusAsync(feed.Id, DateTimeOffset.UtcNow, "Kaynak şu anda okunamadı. URL'yi ve erişimi kontrol edin.", cancellationToken);
                }
            }
            return total;
        }
        finally { _refreshLock.Release(); }
    }

    public async Task<bool> SetStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        if (status is not ("Draft" or "Published" or "Rejected" or "PendingReview")) return false;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.RssNewsItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return false;
        item.Status = status;
        item.ReviewedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetSourceActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var source = await db.RssFeedSources.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (source is null) return false;
        source.IsActive = active;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<int> RefreshFeedAsync(RssFeedSource feed, CancellationToken cancellationToken)
    {
        var uri = await ValidateUrlAsync(feed.FeedUrl, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("NewsPortal/1.0 RSS reader");
        using var response = await clients.CreateClient("rss").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > 2_000_000) throw new InvalidDataException("RSS dosyası 2 MB sınırını aşıyor.");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2_000_000, Async = true };
        using var reader = XmlReader.Create(stream, settings);
        var doc = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        var added = 0;
        var newItemImageLookups = 0;
        var publishedImageLookups = 0;
        var attemptedPublishedImageIds = new HashSet<Guid>();
        foreach (var entry in doc.Descendants().Where(x => x.Name.LocalName is "item" or "entry").Take(100))
        {
            var title = PlainText(Child(entry, "title"));
            var linkElement = entry.Elements().FirstOrDefault(x => x.Name.LocalName == "link");
            var link = (string?)linkElement?.Attribute("href") ?? linkElement?.Value ?? Child(entry, "guid") ?? Child(entry, "id");
            if (string.IsNullOrWhiteSpace(title) || !Uri.TryCreate(link?.Trim(), UriKind.Absolute, out var articleUri) || articleUri.Scheme is not ("http" or "https")) continue;
            var summary = PlainText(Child(entry, "description") ?? Child(entry, "summary") ?? Child(entry, "content"));
            var imageUrl = ExtractImageUrl(entry, uri);
            var externalId = Child(entry, "guid") ?? Child(entry, "id") ?? articleUri.AbsoluteUri;
            var normalizedExternalId = externalId.Trim();
            var dateText = Child(entry, "pubDate") ?? Child(entry, "published") ?? Child(entry, "updated");
            DateTimeOffset? published = DateTimeOffset.TryParse(dateText, out var parsed) ? parsed : null;
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var existing = await db.RssNewsItems.FirstOrDefaultAsync(x => x.FeedId == feed.Id && x.ExternalId == normalizedExternalId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(existing?.ImageUrl)) continue;
            var shouldFetchArticleImage = existing?.Status == "Published"
                ? publishedImageLookups < 20
                : existing is null && newItemImageLookups < 3;
            if (imageUrl is null && shouldFetchArticleImage)
            {
                if (existing?.Status == "Published")
                {
                    publishedImageLookups++;
                    attemptedPublishedImageIds.Add(existing.Id);
                }
                else newItemImageLookups++;
                imageUrl = await FetchArticleImageUrlAsync(articleUri, cancellationToken);
            }
            if (existing is not null)
            {
                if (string.IsNullOrWhiteSpace(existing.ImageUrl) && imageUrl is not null)
                {
                    existing.ImageUrl = imageUrl;
                    await db.SaveChangesAsync(cancellationToken);
                }
                continue;
            }
            db.RssNewsItems.Add(new RssNewsItemEntity
            {
                Id = Guid.NewGuid(), FeedId = feed.Id, ExternalId = normalizedExternalId, SourceName = feed.Name,
                Title = title, Summary = summary, OriginalUrl = articleUri.AbsoluteUri, ImageUrl = imageUrl, Category = feed.Category,
                OriginalPublishedAt = published, ImportedAt = DateTimeOffset.UtcNow, Status = "PendingReview"
            });
            await db.SaveChangesAsync(cancellationToken);
            added++;
        }
        await BackfillPublishedImagesAsync(feed.Id, attemptedPublishedImageIds, cancellationToken);
        await UpdateFeedStatusAsync(feed.Id, DateTimeOffset.UtcNow, null, cancellationToken);
        return added;
    }

    private async Task BackfillPublishedImagesAsync(Guid feedId, HashSet<Guid> attemptedIds, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var missingImages = await db.RssNewsItems.AsNoTracking()
            .Where(x => x.FeedId == feedId && x.Status == "Published" && x.ImageUrl == null && !attemptedIds.Contains(x.Id))
            .OrderByDescending(x => x.OriginalPublishedAt ?? x.ImportedAt)
            .Take(20)
            .Select(x => new { x.Id, x.OriginalUrl })
            .ToArrayAsync(cancellationToken);

        foreach (var item in missingImages)
        {
            if (!Uri.TryCreate(item.OriginalUrl, UriKind.Absolute, out var articleUri)) continue;
            var imageUrl = await FetchArticleImageUrlAsync(articleUri, cancellationToken);
            if (imageUrl is null) continue;
            var existing = await db.RssNewsItems.FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
            if (existing is null || !string.IsNullOrWhiteSpace(existing.ImageUrl)) continue;
            existing.ImageUrl = imageUrl;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<Uri> ValidateUrlAsync(string value, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Geçerli bir HTTP veya HTTPS RSS adresi girin.");
        IPAddress[] addresses = IPAddress.TryParse(uri.Host, out var ip) ? [ip] : await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(IsPrivateAddress)) throw new ArgumentException("Yerel veya özel ağ adreslerine erişime izin verilmiyor.");
        return uri;
    }

    private static bool IsPrivateAddress(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var v = ip.GetAddressBytes();
            return v[0] == 10 || v[0] == 127 || (v[0] == 172 && v[1] is >= 16 and <= 31) || (v[0] == 192 && v[1] == 168) || (v[0] == 169 && v[1] == 254) || v[0] == 0;
        }
        var b = ip.GetAddressBytes();
        return ip.IsIPv6UniqueLocal || ip.IsIPv6SiteLocal || (b[0] == 0xfe && (b[1] & 0xc0) == 0x80);
    }

    private static string? Child(XElement element, string name) => element.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
    private static string? ExtractImageUrl(XElement entry, Uri feedUri)
    {
        var candidate = entry.DescendantsAndSelf()
            .Where(x => x.Name.LocalName is "enclosure" or "content" or "thumbnail")
            .Select(x =>
            {
                var type = (string?)x.Attribute("type");
                if (x.Name.LocalName == "enclosure" && type is not null && !type.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return null;
                return (string?)x.Attribute("url") ?? (string?)x.Attribute("href");
            })
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (string.IsNullOrWhiteSpace(candidate))
        {
            var html = entry.Elements().FirstOrDefault(x => x.Name.LocalName is "description" or "summary" or "encoded")?.Value;
            if (!string.IsNullOrWhiteSpace(html))
            {
                var match = Regex.Match(html, "<img\\b[^>]*?\\b(?:src|data-src)\\s*=\\s*['\\\"]([^'\\\"]+)['\\\"]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match.Success) candidate = WebUtility.HtmlDecode(match.Groups[1].Value);
            }
        }

        if (!Uri.TryCreate(feedUri, candidate, out var imageUri) || imageUri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(imageUri.UserInfo)) return null;
        return imageUri.AbsoluteUri.Length <= 2048 ? imageUri.AbsoluteUri : null;
    }

    private async Task<string?> FetchArticleImageUrlAsync(Uri articleUri, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        var currentUri = articleUri;
        try
        {
            for (var redirect = 0; redirect <= 2; redirect++)
            {
                currentUri = await ValidateUrlAsync(currentUri.AbsoluteUri, timeout.Token);
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                request.Headers.UserAgent.ParseAdd("NewsPortal/1.0 image metadata reader");
                request.Headers.Accept.ParseAdd("text/html");
                using var response = await clients.CreateClient("rss").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null && redirect < 2)
                {
                    currentUri = new Uri(currentUri, response.Headers.Location);
                    continue;
                }
                if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType is not "text/html") return null;
                if (response.Content.Headers.ContentLength is > 1_000_000) return null;

                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var htmlReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var html = await ReadHtmlHeadAsync(htmlReader, timeout.Token);
                foreach (Match tagMatch in Regex.Matches(html, "<meta\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    var tag = tagMatch.Value;
                    var property = HtmlAttribute(tag, "property") ?? HtmlAttribute(tag, "name");
                    if (property is not ("og:image" or "og:image:url" or "twitter:image")) continue;
                    var value = HtmlAttribute(tag, "content");
                    if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(currentUri, value, out var imageUri) || imageUri.Scheme is not ("http" or "https")) continue;
                    try { return (await ValidateUrlAsync(imageUri.AbsoluteUri, timeout.Token)).AbsoluteUri; }
                    catch (Exception ex) when (ex is ArgumentException or SocketException or HttpRequestException) { logger.LogDebug("Haber görsel adresi atlandı: {ImageUrl}", imageUri); }
                }
                return null;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
        catch (Exception ex) when (ex is HttpRequestException or IOException or SocketException or ArgumentException or InvalidDataException)
        {
            logger.LogDebug(ex, "Haber sayfasından görsel alınamadı: {ArticleUrl}", articleUri);
        }
        return null;
    }

    private static async Task<string> ReadHtmlHeadAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        const int maxCharacters = 1_000_000;
        var buffer = new char[8192];
        var result = new StringBuilder();
        while (result.Length < maxCharacters)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maxCharacters - result.Length)), cancellationToken);
            if (count == 0) break;
            result.Append(buffer, 0, count);
            if (result.ToString().Contains("</head", StringComparison.OrdinalIgnoreCase)) break;
        }
        return result.ToString();
    }

    private static string? HtmlAttribute(string tag, string name)
    {
        var doubleQuoted = Regex.Match(tag, $@"\b{Regex.Escape(name)}\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (doubleQuoted.Success) return WebUtility.HtmlDecode(doubleQuoted.Groups[1].Value);
        var singleQuoted = Regex.Match(tag, $@"\b{Regex.Escape(name)}\s*=\s*'([^']*)'", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return singleQuoted.Success ? WebUtility.HtmlDecode(singleQuoted.Groups[1].Value) : null;
    }

    private static string PlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var text = WebUtility.HtmlDecode(Regex.Replace(value, "<[^>]+>", " "));
        return Regex.Replace(text, @"\s+", " ").Trim()[..Math.Min(Regex.Replace(text, @"\s+", " ").Trim().Length, 1000)];
    }
    private static string Slug(string value) => value.Trim().ToLowerInvariant().Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c').Replace(' ', '-');

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SeedSourcesAsync(stoppingToken);
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAllAsync(stoppingToken);
            var minutes = Math.Clamp(configuration.GetValue("Rss:FetchIntervalMinutes", 15), 5, 1440);
            await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
        }
    }

    private async Task SeedSourcesAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (await db.RssFeedSources.AnyAsync(cancellationToken)) return;
        foreach (var (name, url, category) in new[]
        {
            ("TRT Haber – Gündem", "https://www.trthaber.com/gundem_articles.rss", "gundem"),
            ("TRT Haber – Ekonomi", "https://www.trthaber.com/ekonomi_articles.rss", "ekonomi"),
            ("TRT Haber – Spor", "https://www.trthaber.com/spor_articles.rss", "spor"),
            ("TRT Haber – Dünya", "https://www.trthaber.com/dunya_articles.rss", "dunya")
        })
        {
            var id = Guid.NewGuid();
            db.RssFeedSources.Add(new RssFeedSourceEntity { Id = id, Name = name, FeedUrl = url, Category = category, IsActive = true });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateFeedStatusAsync(Guid id, DateTimeOffset fetchedAt, string? error, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var feed = await db.RssFeedSources.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (feed is null) return;
        feed.LastFetchedAt = fetchedAt;
        feed.LastError = error;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static RssFeedSource ToModel(RssFeedSourceEntity source) =>
        new(source.Id, source.Name, source.FeedUrl, source.Category, source.IsActive, source.LastFetchedAt, source.LastError);

    private static RssFeedSourceEntity ToEntity(RssFeedSource source) =>
        new() { Id = source.Id, Name = source.Name, FeedUrl = source.FeedUrl, Category = source.Category, IsActive = source.IsActive, LastFetchedAt = source.LastFetchedAt, LastError = source.LastError };
}
