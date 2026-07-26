namespace CLS.Budget.Domain.CreditCardEngine.Forecast;

public interface IForecastEngine
{
    ForecastResult Generate(ForecastRequest request);
}
