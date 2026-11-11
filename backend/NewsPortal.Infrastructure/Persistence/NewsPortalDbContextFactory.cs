using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NewsPortal.Infrastructure.Persistence;

public sealed class NewsPortalDbContextFactory : IDesignTimeDbContextFactory<NewsPortalDbContext>
{
    public NewsPortalDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=NewsPortal;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<NewsPortalDbContext>().UseSqlServer(connectionString).Options;
        return new NewsPortalDbContext(options);
    }
}
