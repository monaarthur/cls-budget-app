using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IPayFrequencyTypeRepository
{
    Task<IReadOnlyList<PayFrequencyType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int payFrequencyTypeId, CancellationToken cancellationToken = default);
}

public interface IIncomeSourceRepository
{
    Task<IReadOnlyList<IncomeSource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IncomeSource?> GetByIdAsync(int incomeSourceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int incomeSourceId, CancellationToken cancellationToken = default);
}

public interface IPayScheduleRepository
{
    Task<IReadOnlyList<PaySchedule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaySchedule?> GetByIdAsync(int payScheduleId, CancellationToken cancellationToken = default);
    Task<PaySchedule?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<PaySchedule> AddAsync(PaySchedule schedule, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaySchedule schedule, CancellationToken cancellationToken = default);
    Task ClearDefaultFlagAsync(CancellationToken cancellationToken = default);
}
