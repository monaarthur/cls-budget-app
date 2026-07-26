namespace CLS.Budget.Domain.Entities;

public class ForecastScenario : ITenantOwned
{
    public int ForecastScenarioId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public decimal TotalMonthlyDebtPayment { get; set; }
    public DateOnly StartDate { get; set; }
    public int ForecastMonths { get; set; }
    public decimal StartingDebt { get; set; }
    public decimal? MonthlyNetIncome { get; set; }
    public decimal? MonthlyExpenses { get; set; }
    public decimal? TargetUtilizationPercent { get; set; }
    public bool PayOverLimitFirst { get; set; }
    public DateOnly? EstimatedDebtFreeDate { get; set; }
    public decimal TotalInterestPaid { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public ICollection<ForecastScenarioCreditCard> CreditCards { get; set; } = [];
    public ICollection<ForecastMonthlySnapshot> MonthlySnapshots { get; set; } = [];
}
