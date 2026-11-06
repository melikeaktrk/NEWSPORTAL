namespace NewsPortal.Infrastructure.Persistence;

public sealed class RssFeedSourceEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string FeedUrl { get; set; } = "";
    public string Category { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTimeOffset? LastFetchedAt { get; set; }
    public string? LastError { get; set; }
    public ICollection<RssNewsItemEntity> Items { get; set; } = new List<RssNewsItemEntity>();
}

public sealed class RssNewsItemEntity
{
    public Guid Id { get; set; }
    public Guid FeedId { get; set; }
    public RssFeedSourceEntity Feed { get; set; } = null!;
    public string ExternalId { get; set; } = "";
    public string SourceName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string OriginalUrl { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTimeOffset? OriginalPublishedAt { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset? ReviewedAt { get; set; }
}
