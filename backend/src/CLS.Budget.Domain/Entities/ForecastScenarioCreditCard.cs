namespace CLS.Budget.Domain.Entities;

public class ForecastScenarioCreditCard : ITenantOwned
{
    public int ForecastScenarioCreditCardId { get; set; }
    public Guid TenantId { get; set; }
    public int ForecastScenarioId { get; set; }
    public ForecastScenario? ForecastScenario { get; set; }
    public int CreditCardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal StartingBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal AnnualPercentageRate { get; set; }
}
