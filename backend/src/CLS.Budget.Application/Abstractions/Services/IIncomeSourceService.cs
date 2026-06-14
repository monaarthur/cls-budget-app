using CLS.Budget.Application.Common;
using CLS.Budget.Application.IncomeSources.Dtos;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IIncomeSourceService
{
    Task<ApiResponse<IReadOnlyList<IncomeSourceResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
