namespace CLS.Budget.Domain.Entities;

public class PaySchedule : ITenantOwned
{
    public int PayScheduleId { get; set; }
    public Guid TenantId { get; set; }
    public int IncomeSourceId { get; set; }
    public IncomeSource? IncomeSource { get; set; }
    public int PayFrequencyTypeId { get; set; }
    public PayFrequencyType? PayFrequencyType { get; set; }
    public string Name { get; set; } = null!;
    public DateTime? AnchorDate { get; set; }
    public int? DayOfWeek { get; set; }
    public int? SemiMonthlyDay1 { get; set; }
    public int? SemiMonthlyDay2 { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
