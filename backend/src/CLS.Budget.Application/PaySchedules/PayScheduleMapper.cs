using CLS.Budget.Application.PaySchedules.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.PaySchedules;

internal static class PayScheduleMapper
{
    public static PayFrequencyTypeResponse ToResponse(PayFrequencyType type) => new()
    {
        PayFrequencyTypeId = type.PayFrequencyTypeId,
        Name = type.Name,
        Description = type.Description
    };

    public static PayScheduleResponse ToResponse(PaySchedule schedule) => new()
    {
        PayScheduleId = schedule.PayScheduleId,
        IncomeSourceId = schedule.IncomeSourceId,
        IncomeSourceName = schedule.IncomeSource?.Name ?? string.Empty,
        PayFrequencyTypeId = schedule.PayFrequencyTypeId,
        PayFrequencyTypeName = schedule.PayFrequencyType?.Name ?? string.Empty,
        Name = schedule.Name,
        AnchorDate = schedule.AnchorDate,
        DayOfWeek = schedule.DayOfWeek,
        SemiMonthlyDay1 = schedule.SemiMonthlyDay1,
        SemiMonthlyDay2 = schedule.SemiMonthlyDay2,
        IsDefault = schedule.IsDefault,
        IsActive = schedule.IsActive
    };

    public static PaySchedule ToEntity(CreatePayScheduleRequest request) => new()
    {
        IncomeSourceId = request.IncomeSourceId,
        PayFrequencyTypeId = request.PayFrequencyTypeId,
        Name = request.Name,
        AnchorDate = request.AnchorDate,
        DayOfWeek = request.DayOfWeek,
        SemiMonthlyDay1 = request.SemiMonthlyDay1,
        SemiMonthlyDay2 = request.SemiMonthlyDay2,
        IsDefault = request.IsDefault,
        IsActive = request.IsActive
    };

    public static void ApplyUpdate(PaySchedule schedule, UpdatePayScheduleRequest request)
    {
        schedule.IncomeSourceId = request.IncomeSourceId;
        schedule.PayFrequencyTypeId = request.PayFrequencyTypeId;
        schedule.Name = request.Name;
        schedule.AnchorDate = request.AnchorDate;
        schedule.DayOfWeek = request.DayOfWeek;
        schedule.SemiMonthlyDay1 = request.SemiMonthlyDay1;
        schedule.SemiMonthlyDay2 = request.SemiMonthlyDay2;
        schedule.IsDefault = request.IsDefault;
        schedule.IsActive = request.IsActive;
    }
}
