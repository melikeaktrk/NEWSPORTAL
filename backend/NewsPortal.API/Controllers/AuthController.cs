using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NewsPortal.Infrastructure.Persistence;

namespace NewsPortal.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(SignInManager<NewsPortalUser> signInManager, UserManager<NewsPortalUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || !await userManager.IsInRoleAsync(user, "SuperAdmin"))
            return Unauthorized(new { success = false, message = "E-posta veya parola hatalı." });

        var result = await signInManager.PasswordSignInAsync(user.UserName!, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized(new { success = false, message = "E-posta veya parola hatalı." });
        return Ok(new { success = true, data = new { email = user.Email, displayName = user.DisplayName } });
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null
            ? Unauthorized(new { success = false, message = "Oturum geçersiz." })
            : Ok(new { success = true, data = new { email = user.Email, displayName = user.DisplayName, role = "SuperAdmin" } });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new { success = true, message = "Oturum kapatıldı." });
    }

    public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);
}
