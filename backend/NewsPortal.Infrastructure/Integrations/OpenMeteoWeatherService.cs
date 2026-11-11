using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using NewsPortal.Application.Weather;

namespace NewsPortal.Infrastructure.Integrations;

public sealed class OpenMeteoWeatherService(HttpClient http, IMemoryCache cache) : IWeatherService
{
    private static readonly string[] TurkishProvinces =
    [
        "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya", "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum", "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir", "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır", "Isparta", "İstanbul", "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kırıkkale", "Kırklareli", "Kırşehir", "Kilis", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa", "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye", "Rize", "Sakarya", "Samsun", "Siirt", "Sinop", "Sivas", "Şanlıurfa", "Şırnak", "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak"
    ];

    public IReadOnlyList<string> GetCities() => TurkishProvinces;

    public async Task<WeatherSnapshot> GetAsync(string city, CancellationToken cancellationToken)
    {
        var canonicalCity = TurkishProvinces.FirstOrDefault(x => x.Equals(city.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonicalCity is null) throw new ArgumentException("Türkiye'nin 81 ilinden birini seçin.", nameof(city));
        var coordinates = await GetCoordinatesAsync(canonicalCity, cancellationToken);
        var key = $"weather:{canonicalCity}";
        if (cache.TryGetValue(key, out WeatherSnapshot? cached) && cached is not null) return cached;

        var latitude = coordinates.Lat.ToString(CultureInfo.InvariantCulture);
        var longitude = coordinates.Lon.ToString(CultureInfo.InvariantCulture);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,apparent_temperature,relative_humidity_2m,wind_speed_10m,weather_code&daily=temperature_2m_min,temperature_2m_max&timezone=auto&forecast_days=1";
        var response = await http.GetFromJsonAsync<ForecastResponse>(url, cancellationToken)
            ?? throw new HttpRequestException("Hava durumu sağlayıcısından boş yanıt alındı.");
        if (response.Current is null) throw new HttpRequestException("Hava durumu yanıtında güncel veri bulunamadı.");

        var data = new WeatherSnapshot(canonicalCity, response.Current.Temperature, response.Current.ApparentTemperature,
            response.Current.RelativeHumidity, response.Current.WindSpeed, response.Current.WeatherCode, Describe(response.Current.WeatherCode),
            response.Daily?.Min?.FirstOrDefault(), response.Daily?.Max?.FirstOrDefault(), response.Daily?.Time?.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DateTimeOffset.UtcNow, "Open-Meteo");
        cache.Set(key, data, TimeSpan.FromMinutes(20));
        return data;
    }

    private async Task<(double Lat, double Lon)> GetCoordinatesAsync(string city, CancellationToken cancellationToken)
    {
        var key = $"weather-coordinates:{city}";
        if (cache.TryGetValue(key, out (double Lat, double Lon) cached)) return cached;
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=10&language=tr&format=json&countryCode=TR";
        var response = await http.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);
        var place = response?.Results?.FirstOrDefault(x => x.CountryCode == "TR" && (Normalize(x.Name) == Normalize(city) || Normalize(x.Admin1 ?? "") == Normalize(city)))
            ?? response?.Results?.FirstOrDefault(x => x.CountryCode == "TR");
        if (place is null) throw new HttpRequestException($"{city} için konum bulunamadı.");
        var result = (place.Latitude, place.Longitude);
        cache.Set(key, result, TimeSpan.FromDays(30));
        return result;
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        return builder.ToString();
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
        [JsonPropertyName("temperature_2m")] public double Temperature { get; set; }
        [JsonPropertyName("apparent_temperature")] public double? ApparentTemperature { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public int? RelativeHumidity { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double? WindSpeed { get; set; }
        [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
    }
    private sealed class DailyData
    {
        [JsonPropertyName("time")] public List<string>? Time { get; set; }
        [JsonPropertyName("temperature_2m_min")] public List<double>? Min { get; set; }
        [JsonPropertyName("temperature_2m_max")] public List<double>? Max { get; set; }
    }
    private sealed class GeocodingResponse { [JsonPropertyName("results")] public List<GeocodedPlace>? Results { get; set; } }
    private sealed class GeocodedPlace
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("admin1")] public string? Admin1 { get; set; }
        [JsonPropertyName("country_code")] public string CountryCode { get; set; } = "";
        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
    }
}
