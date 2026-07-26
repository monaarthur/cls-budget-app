namespace CLS.Budget.Domain.Entities;

public class ForecastMonthlySnapshot : ITenantOwned
{
    public int ForecastMonthlySnapshotId { get; set; }
    public Guid TenantId { get; set; }
    public int ForecastScenarioId { get; set; }
    public ForecastScenario? ForecastScenario { get; set; }
    public DateOnly Month { get; set; }
    public int MonthIndex { get; set; }
    public decimal StartingDebt { get; set; }
    public decimal NewCharges { get; set; }
    public decimal Interest { get; set; }
    public decimal Payments { get; set; }
    public decimal EndingDebt { get; set; }
    public decimal TotalCreditLimit { get; set; }
    public decimal OverallUtilizationPercentage { get; set; }
    public decimal AvailableCash { get; set; }
    public int CardsPaidOffThisMonth { get; set; }
    public decimal CumulativeInterest { get; set; }
}
