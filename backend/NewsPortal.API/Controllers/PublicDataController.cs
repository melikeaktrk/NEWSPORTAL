using Microsoft.AspNetCore.Mvc;
using NewsPortal.Application.ExchangeRates;
using NewsPortal.Application.Weather;

namespace NewsPortal.API.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicDataController(IWeatherService weather, IExchangeRateService rates) : ControllerBase
{
    [HttpGet("weather/cities")]
    public IActionResult GetCities() => Ok(new { success = true, data = weather.GetCities() });

    [HttpGet("weather")]
    public async Task<IActionResult> GetWeather([FromQuery] string city = "Malatya", CancellationToken cancellationToken = default)
    {
        try { return Ok(new { success = true, data = await weather.GetAsync(city, cancellationToken) }); }
        catch (ArgumentException exception) { return BadRequest(new { success = false, message = exception.Message }); }
        catch (HttpRequestException) { return StatusCode(503, new { success = false, message = "Hava durumu verisi şu anda alınamıyor." }); }
    }

    [HttpGet("exchange-rates")]
    public async Task<IActionResult> GetRates(CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Günlük referans kurlarıdır; canlı alış/satış fiyatı değildir.", data = await rates.GetAsync(cancellationToken) });
}
