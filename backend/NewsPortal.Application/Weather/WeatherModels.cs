namespace NewsPortal.Application.Weather;

public sealed record WeatherSnapshot(
    string City,
    double Temperature,
    double? ApparentTemperature,
    int? RelativeHumidity,
    double? WindSpeed,
    int WeatherCode,
    string Description,
    double? DailyMinTemperature,
    double? DailyMaxTemperature,
    string ForecastDate,
    DateTimeOffset FetchedAt,
    string Source);

public interface IWeatherService
{
    IReadOnlyList<string> GetCities();
    Task<WeatherSnapshot> GetAsync(string city, CancellationToken cancellationToken);
}
