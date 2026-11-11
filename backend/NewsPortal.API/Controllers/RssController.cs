using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NewsPortal.Infrastructure.Feeds;

namespace NewsPortal.API.Controllers;

[ApiController]
[Route("api")]
public sealed class RssController(RssNewsService rss) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/news")]
    public async Task<IActionResult> GetPublished([FromQuery] string? category = null, CancellationToken cancellationToken = default) => Ok(new
    {
        success = true,
        data = await rss.GetItemsAsync("Published", category, cancellationToken)
    });

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/rss/sources")]
    public async Task<IActionResult> GetSources(CancellationToken cancellationToken)
    {
        return Ok(new { success = true, data = await rss.GetSourcesAsync(cancellationToken) });
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/rss/items")]
    public async Task<IActionResult> GetPendingItems([FromQuery] string status = "PendingReview", CancellationToken cancellationToken = default)
    {
        if (status is not ("PendingReview" or "Published" or "Rejected")) return BadRequest(new { success = false, message = "Geçersiz içerik durumu." });
        return Ok(new { success = true, data = await rss.GetItemsAsync(status, cancellationToken: cancellationToken) });
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/rss/refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var added = await rss.RefreshAllAsync(cancellationToken);
        return Ok(new { success = true, message = $"{added} yeni kayıt inceleme listesine eklendi.", data = await rss.GetItemsAsync("PendingReview", cancellationToken: cancellationToken) });
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/rss/sources")]
    public async Task<IActionResult> AddSource([FromBody] AddSourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var source = await rss.AddSourceAsync(request.Name, request.FeedUrl, request.Category, cancellationToken);
            return Ok(new { success = true, data = source });
        }
        catch (ArgumentException exception) { return BadRequest(new { success = false, message = exception.Message }); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/rss/items/{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusRequest request, CancellationToken cancellationToken)
    {
        return await rss.SetStatusAsync(id, request.Status, cancellationToken)
            ? Ok(new { success = true, message = "Haber durumu güncellendi." })
            : BadRequest(new { success = false, message = "Haber bulunamadı veya durum geçersiz." });
    }

    public sealed record AddSourceRequest(string Name, string FeedUrl, string Category);
    public sealed record StatusRequest(string Status);
    public sealed record ActiveRequest(bool IsActive);

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/rss/sources/{id:guid}/active")]
    public async Task<IActionResult> SetSourceActive(Guid id, [FromBody] ActiveRequest request, CancellationToken cancellationToken)
    {
        return await rss.SetSourceActiveAsync(id, request.IsActive, cancellationToken)
            ? Ok(new { success = true, message = request.IsActive ? "Kaynak etkinleştirildi." : "Kaynak duraklatıldı." })
            : NotFound(new { success = false, message = "Kaynak bulunamadı." });
    }
}
