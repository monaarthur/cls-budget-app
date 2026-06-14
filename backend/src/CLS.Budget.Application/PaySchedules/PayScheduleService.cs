using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.PaySchedules.Dtos;

namespace CLS.Budget.Application.PaySchedules;

public sealed class PayFrequencyTypeService(IPayFrequencyTypeRepository payFrequencyTypeRepository)
    : IPayFrequencyTypeService
{
    public async Task<ApiResponse<IReadOnlyList<PayFrequencyTypeResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var types = await payFrequencyTypeRepository.GetAllAsync(cancellationToken);
        var data = types.Select(PayScheduleMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<PayFrequencyTypeResponse>>.Ok(data);
    }
}

public sealed class PayScheduleService(
    IPayScheduleRepository payScheduleRepository,
    IPayFrequencyTypeRepository payFrequencyTypeRepository,
    IIncomeSourceRepository incomeSourceRepository) : IPayScheduleService
{
    public async Task<ApiResponse<IReadOnlyList<PayScheduleResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var schedules = await payScheduleRepository.GetAllAsync(cancellationToken);
        var data = schedules.Select(PayScheduleMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<PayScheduleResponse>>.Ok(data);
    }

    public async Task<ApiResponse<PayScheduleResponse>> GetByIdAsync(
        int payScheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await payScheduleRepository.GetByIdAsync(payScheduleId, cancellationToken);
        if (schedule is null)
        {
            return ApiResponse<PayScheduleResponse>.Fail(
                $"Pay schedule with id {payScheduleId} was not found.");
        }

        return ApiResponse<PayScheduleResponse>.Ok(PayScheduleMapper.ToResponse(schedule));
    }

    public async Task<ApiResponse<PayScheduleResponse>> GetDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        var schedule = await payScheduleRepository.GetDefaultAsync(cancellationToken);
        if (schedule is null)
        {
            return ApiResponse<PayScheduleResponse>.Fail("No default pay schedule was found.");
        }

        return ApiResponse<PayScheduleResponse>.Ok(PayScheduleMapper.ToResponse(schedule));
    }

    public async Task<ApiResponse<PayScheduleResponse>> CreateAsync(
        CreatePayScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateReferencesAsync(
            request.IncomeSourceId,
            request.PayFrequencyTypeId,
            cancellationToken);
        if (validationError is not null)
        {
            return ApiResponse<PayScheduleResponse>.Fail(validationError);
        }

        if (request.IsDefault)
        {
            await payScheduleRepository.ClearDefaultFlagAsync(cancellationToken);
        }

        var created = await payScheduleRepository.AddAsync(
            PayScheduleMapper.ToEntity(request),
            cancellationToken);
        var loaded = await payScheduleRepository.GetByIdAsync(created.PayScheduleId, cancellationToken);
        return ApiResponse<PayScheduleResponse>.Ok(PayScheduleMapper.ToResponse(loaded!));
    }

    public async Task<ApiResponse<PayScheduleResponse>> UpdateAsync(
        int payScheduleId,
        UpdatePayScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var schedule = await payScheduleRepository.GetByIdAsync(payScheduleId, cancellationToken);
        if (schedule is null)
        {
            return ApiResponse<PayScheduleResponse>.Fail(
                $"Pay schedule with id {payScheduleId} was not found.");
        }

        var validationError = await ValidateReferencesAsync(
            request.IncomeSourceId,
            request.PayFrequencyTypeId,
            cancellationToken);
        if (validationError is not null)
        {
            return ApiResponse<PayScheduleResponse>.Fail(validationError);
        }

        if (request.IsDefault && !schedule.IsDefault)
        {
            await payScheduleRepository.ClearDefaultFlagAsync(cancellationToken);
        }

        PayScheduleMapper.ApplyUpdate(schedule, request);
        await payScheduleRepository.UpdateAsync(schedule, cancellationToken);
        var updated = await payScheduleRepository.GetByIdAsync(payScheduleId, cancellationToken);
        return ApiResponse<PayScheduleResponse>.Ok(PayScheduleMapper.ToResponse(updated!));
    }

    public async Task<ApiResponse<PayScheduleDatesResponse>> GetDatesAsync(
        int payScheduleId,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken = default)
    {
        var schedule = await payScheduleRepository.GetByIdAsync(payScheduleId, cancellationToken);
        if (schedule is null)
        {
            return ApiResponse<PayScheduleDatesResponse>.Fail(
                $"Pay schedule with id {payScheduleId} was not found.");
        }

        var payDates = PayDateCalculator.GetPayDatesInRange(schedule, rangeStart, rangeEnd);
        var periods = PayDateCalculator.GetPayPeriodBoundaries(schedule, rangeStart, rangeEnd);

        return ApiResponse<PayScheduleDatesResponse>.Ok(new PayScheduleDatesResponse
        {
            PayScheduleId = payScheduleId,
            RangeStart = PayDateCalculator.ToUtcDate(rangeStart),
            RangeEnd = PayDateCalculator.ToUtcDate(rangeEnd),
            PayDates = payDates,
            Periods = periods.Select(p => new PayPeriodBoundaryResponse
            {
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd,
                Label = p.Label
            }).ToList()
        });
    }

    public async Task<ApiResponse<PayScheduleResponse>> ResolveForBudgetAsync(
        int? payScheduleId,
        CancellationToken cancellationToken = default)
    {
        if (payScheduleId.HasValue)
        {
            return await GetByIdAsync(payScheduleId.Value, cancellationToken);
        }

        return await GetDefaultAsync(cancellationToken);
    }

    private async Task<string?> ValidateReferencesAsync(
        int incomeSourceId,
        int payFrequencyTypeId,
        CancellationToken cancellationToken)
    {
        if (!await incomeSourceRepository.ExistsAsync(incomeSourceId, cancellationToken))
        {
            return $"Income source with id {incomeSourceId} was not found.";
        }

        if (!await payFrequencyTypeRepository.ExistsAsync(payFrequencyTypeId, cancellationToken))
        {
            return $"Pay frequency type with id {payFrequencyTypeId} was not found.";
        }

        return null;
    }
}
