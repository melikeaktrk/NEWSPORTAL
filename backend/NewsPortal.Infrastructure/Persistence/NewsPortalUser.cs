using Microsoft.AspNetCore.Identity;

namespace NewsPortal.Infrastructure.Persistence;

public sealed class NewsPortalUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
