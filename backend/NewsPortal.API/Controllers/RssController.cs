using Microsoft.AspNetCore.Mvc;
using NewsPortal.Infrastructure.Feeds;

namespace NewsPortal.API.Controllers;

[ApiController]
[Route("api")]
public sealed class RssController(RssNewsService rss, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("public/news")]
    public async Task<IActionResult> GetPublished([FromQuery] string? category = null, CancellationToken cancellationToken = default) => Ok(new
    {
        success = true,
        data = await rss.GetItemsAsync("Published", category, cancellationToken)
    });

    [HttpGet("admin/rss/sources")]
    public async Task<IActionResult> GetSources(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        return Ok(new { success = true, data = await rss.GetSourcesAsync(cancellationToken) });
    }

    [HttpGet("admin/rss/items")]
    public async Task<IActionResult> GetPendingItems([FromQuery] string status = "PendingReview", CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (status is not ("PendingReview" or "Published" or "Rejected")) return BadRequest(new { success = false, message = "Geçersiz içerik durumu." });
        return Ok(new { success = true, data = await rss.GetItemsAsync(status, cancellationToken: cancellationToken) });
    }

    [HttpPost("admin/rss/refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var added = await rss.RefreshAllAsync(cancellationToken);
        return Ok(new { success = true, message = $"{added} yeni kayıt inceleme listesine eklendi.", data = await rss.GetItemsAsync("PendingReview", cancellationToken: cancellationToken) });
    }

    [HttpPost("admin/rss/sources")]
    public async Task<IActionResult> AddSource([FromBody] AddSourceRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        try
        {
            var source = await rss.AddSourceAsync(request.Name, request.FeedUrl, request.Category, cancellationToken);
            return Ok(new { success = true, data = source });
        }
        catch (ArgumentException exception) { return BadRequest(new { success = false, message = exception.Message }); }
    }

    [HttpPost("admin/rss/items/{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        return await rss.SetStatusAsync(id, request.Status, cancellationToken)
            ? Ok(new { success = true, message = "Haber durumu güncellendi." })
            : BadRequest(new { success = false, message = "Haber bulunamadı veya durum geçersiz." });
    }

    public sealed record AddSourceRequest(string Name, string FeedUrl, string Category);
    public sealed record StatusRequest(string Status);
    public sealed record ActiveRequest(bool IsActive);

    [HttpPost("admin/rss/sources/{id:guid}/active")]
    public async Task<IActionResult> SetSourceActive(Guid id, [FromBody] ActiveRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        return await rss.SetSourceActiveAsync(id, request.IsActive, cancellationToken)
            ? Ok(new { success = true, message = request.IsActive ? "Kaynak etkinleştirildi." : "Kaynak duraklatıldı." })
            : NotFound(new { success = false, message = "Kaynak bulunamadı." });
    }
}
