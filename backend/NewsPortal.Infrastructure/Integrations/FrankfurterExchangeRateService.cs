using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using NewsPortal.Application.ExchangeRates;

namespace NewsPortal.Infrastructure.Integrations;

public sealed class FrankfurterExchangeRateService(HttpClient http, IMemoryCache cache) : IExchangeRateService
{
    public async Task<IReadOnlyList<ExchangeRate>> GetAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue("exchange-rates:TRY", out IReadOnlyList<ExchangeRate>? cached) && cached is not null) return cached;
        var currencies = new[] { "USD", "EUR", "GBP" };
        var tasks = currencies.Select(currency => FetchAsync(currency, cancellationToken));
        var rates = (await Task.WhenAll(tasks)).ToArray();
        cache.Set("exchange-rates:TRY", rates, TimeSpan.FromHours(8));
        return rates;
    }

    private async Task<ExchangeRate> FetchAsync(string baseCurrency, CancellationToken cancellationToken)
    {
        var rows = await http.GetFromJsonAsync<List<RateRow>>($"https://api.frankfurter.dev/v2/rates?base={baseCurrency}&quotes=TRY", cancellationToken)
            ?? throw new HttpRequestException("Döviz sağlayıcısından boş yanıt alındı.");
        var row = rows.FirstOrDefault() ?? throw new HttpRequestException($"{baseCurrency}/TRY kuru yanıt içinde bulunamadı.");
        return new ExchangeRate(row.Base, row.Quote, row.Rate, DateOnly.Parse(row.Date), DateTimeOffset.UtcNow, "Frankfurter (günlük referans kuru)");
    }

    private sealed class RateRow { public string Date { get; set; } = ""; public string Base { get; set; } = ""; public string Quote { get; set; } = ""; public decimal Rate { get; set; } }
}
