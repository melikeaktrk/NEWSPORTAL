using Microsoft.EntityFrameworkCore;

namespace NewsPortal.Infrastructure.Persistence;

public sealed class NewsPortalDbContext(DbContextOptions<NewsPortalDbContext> options) : DbContext(options)
{
    public DbSet<RssFeedSourceEntity> RssFeedSources => Set<RssFeedSourceEntity>();
    public DbSet<RssNewsItemEntity> RssNewsItems => Set<RssNewsItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RssFeedSourceEntity>(entity =>
        {
            entity.ToTable("RssFeedSources");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FeedUrl).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(1000);
            entity.HasIndex(x => x.FeedUrl).IsUnique();
        });

        modelBuilder.Entity<RssNewsItemEntity>(entity =>
        {
            entity.ToTable("RssNewsItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.OriginalUrl).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.FeedId, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.Category, x.OriginalPublishedAt });
            entity.HasOne(x => x.Feed)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.FeedId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
