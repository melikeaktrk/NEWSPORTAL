using System.Collections.Concurrent;
using System.Net;
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
public sealed record RssNewsItem(Guid Id, Guid FeedId, string SourceName, string Title, string Summary, string OriginalUrl, string Category, DateTimeOffset? OriginalPublishedAt, DateTimeOffset ImportedAt, string Status);

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
            .Select(x => new RssNewsItem(x.Id, x.FeedId, x.SourceName, x.Title, x.Summary, x.OriginalUrl, x.Category, x.OriginalPublishedAt, x.ImportedAt, x.Status))
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
        foreach (var entry in doc.Descendants().Where(x => x.Name.LocalName is "item" or "entry").Take(100))
        {
            var title = PlainText(Child(entry, "title"));
            var linkElement = entry.Elements().FirstOrDefault(x => x.Name.LocalName == "link");
            var link = (string?)linkElement?.Attribute("href") ?? linkElement?.Value ?? Child(entry, "guid") ?? Child(entry, "id");
            if (string.IsNullOrWhiteSpace(title) || !Uri.TryCreate(link?.Trim(), UriKind.Absolute, out var articleUri) || articleUri.Scheme is not ("http" or "https")) continue;
            var summary = PlainText(Child(entry, "description") ?? Child(entry, "summary") ?? Child(entry, "content"));
            var externalId = Child(entry, "guid") ?? Child(entry, "id") ?? articleUri.AbsoluteUri;
            var normalizedExternalId = externalId.Trim();
            var dateText = Child(entry, "pubDate") ?? Child(entry, "published") ?? Child(entry, "updated");
            DateTimeOffset? published = DateTimeOffset.TryParse(dateText, out var parsed) ? parsed : null;
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            if (await db.RssNewsItems.AnyAsync(x => x.FeedId == feed.Id && x.ExternalId == normalizedExternalId, cancellationToken)) continue;
            db.RssNewsItems.Add(new RssNewsItemEntity
            {
                Id = Guid.NewGuid(), FeedId = feed.Id, ExternalId = normalizedExternalId, SourceName = feed.Name,
                Title = title, Summary = summary, OriginalUrl = articleUri.AbsoluteUri, Category = feed.Category,
                OriginalPublishedAt = published, ImportedAt = DateTimeOffset.UtcNow, Status = "PendingReview"
            });
            await db.SaveChangesAsync(cancellationToken);
            added++;
        }
        await UpdateFeedStatusAsync(feed.Id, DateTimeOffset.UtcNow, null, cancellationToken);
        return added;
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
