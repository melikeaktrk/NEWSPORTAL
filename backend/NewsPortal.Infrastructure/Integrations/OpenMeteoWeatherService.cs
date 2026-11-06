using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using NewsPortal.Application.Weather;

namespace NewsPortal.Infrastructure.Integrations;

public sealed class OpenMeteoWeatherService(HttpClient http, IMemoryCache cache) : IWeatherService
{
    private static readonly IReadOnlyDictionary<string, (double Lat, double Lon)> Cities = new Dictionary<string, (double, double)>(StringComparer.OrdinalIgnoreCase)
    {
        ["İstanbul"] = (41.0082, 28.9784), ["Ankara"] = (39.9334, 32.8597), ["İzmir"] = (38.4237, 27.1428),
        ["Malatya"] = (38.3552, 38.3095), ["Antalya"] = (36.8969, 30.7133), ["Bursa"] = (40.1885, 29.0610), ["Adana"] = (37.0000, 35.3213)
    };

    public async Task<WeatherSnapshot> GetAsync(string city, CancellationToken cancellationToken)
    {
        if (!Cities.TryGetValue(city.Trim(), out var coordinates))
            throw new ArgumentException("Desteklenmeyen şehir. İstanbul, Ankara, İzmir, Malatya, Antalya, Bursa veya Adana seçin.", nameof(city));

        var key = $"weather:{city}";
        if (cache.TryGetValue(key, out WeatherSnapshot? cached) && cached is not null) return cached;

        var latitude = coordinates.Lat.ToString(CultureInfo.InvariantCulture);
        var longitude = coordinates.Lon.ToString(CultureInfo.InvariantCulture);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,apparent_temperature,relative_humidity_2m,wind_speed_10m,weather_code&daily=temperature_2m_min,temperature_2m_max&timezone=auto&forecast_days=1";
        var response = await http.GetFromJsonAsync<ForecastResponse>(url, cancellationToken)
            ?? throw new HttpRequestException("Hava durumu sağlayıcısından boş yanıt alındı.");
        if (response.Current is null) throw new HttpRequestException("Hava durumu yanıtında güncel veri bulunamadı.");

        var data = new WeatherSnapshot(city, response.Current.Temperature, response.Current.ApparentTemperature,
            response.Current.RelativeHumidity, response.Current.WindSpeed, response.Current.WeatherCode, Describe(response.Current.WeatherCode),
            response.Daily?.Min?.FirstOrDefault(), response.Daily?.Max?.FirstOrDefault(), response.Daily?.Time?.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DateTimeOffset.UtcNow, "Open-Meteo");
        cache.Set(key, data, TimeSpan.FromMinutes(20));
        return data;
    }

    private static string Describe(int code) => code switch
    {
        0 => "Açık", 1 => "Çoğunlukla açık", 2 => "Parçalı bulutlu", 3 => "Kapalı",
        45 or 48 => "Sisli", 51 or 53 or 55 => "Çisenti", 56 or 57 => "Dondurucu çisenti",
        61 or 63 or 65 => "Yağmurlu", 66 or 67 => "Dondurucu yağmur", 71 or 73 or 75 or 77 => "Karlı",
        80 or 81 or 82 => "Sağanak", 85 or 86 => "Kar sağanağı", 95 or 96 or 99 => "Gök gürültülü fırtına",
        _ => "Hava durumu"
    };

    private sealed class ForecastResponse { public CurrentData? Current { get; set; } public DailyData? Daily { get; set; } }
    private sealed class CurrentData
    {
        [JsonPropertyName("time")] public string? Time { get; set; }
        [JsonPropertyName("temperature_2m")]
        public double Temperature { get; set; }
        [JsonPropertyName("apparent_temperature")]
        public double? ApparentTemperature { get; set; }
        [JsonPropertyName("relative_humidity_2m")]
        public int? RelativeHumidity { get; set; }
        [JsonPropertyName("wind_speed_10m")]
        public double? WindSpeed { get; set; }
        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }
    private sealed class DailyData
    {
        [JsonPropertyName("time")] public List<string>? Time { get; set; }
        [JsonPropertyName("temperature_2m_min")] public List<double>? Min { get; set; }
        [JsonPropertyName("temperature_2m_max")] public List<double>? Max { get; set; }
    }
}
