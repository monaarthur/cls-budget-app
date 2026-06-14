using CLS.Budget.Application.PaySchedules.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IPayFrequencyTypeService
{
    Task<ApiResponse<IReadOnlyList<PayFrequencyTypeResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

public interface IPayScheduleService
{
    Task<ApiResponse<IReadOnlyList<PayScheduleResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PayScheduleResponse>> GetByIdAsync(
        int payScheduleId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PayScheduleResponse>> GetDefaultAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PayScheduleResponse>> CreateAsync(
        CreatePayScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PayScheduleResponse>> UpdateAsync(
        int payScheduleId,
        UpdatePayScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PayScheduleDatesResponse>> GetDatesAsync(
        int payScheduleId,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PayScheduleResponse>> ResolveForBudgetAsync(
        int? payScheduleId,
        CancellationToken cancellationToken = default);
}
