namespace NewsPortal.Application.ExchangeRates;

public sealed record ExchangeRate(string BaseCurrency, string TargetCurrency, decimal Rate, DateOnly RateDate, DateTimeOffset FetchedAt, string Source);

public interface IExchangeRateService
{
    Task<IReadOnlyList<ExchangeRate>> GetAsync(CancellationToken cancellationToken);
}
