namespace CLS.Budget.Domain.Entities;

public static class ActivePayoffPlanStatuses
{
    public const string Active = "Active";
    public const string Completed = "Completed";
    public const string Abandoned = "Abandoned";
}

public static class PayoffPlanEventTypes
{
    public const string Started = "Started";
    public const string PaymentRecorded = "PaymentRecorded";
    public const string PaymentVoided = "PaymentVoided";
    public const string Revised = "Revised";
    public const string Completed = "Completed";
    public const string Abandoned = "Abandoned";
}
