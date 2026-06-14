using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.IncomeSources.Dtos;

namespace CLS.Budget.Application.IncomeSources;

public sealed class IncomeSourceService(IIncomeSourceRepository incomeSourceRepository) : IIncomeSourceService
{
    public async Task<ApiResponse<IReadOnlyList<IncomeSourceResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await incomeSourceRepository.GetAllAsync(cancellationToken);
        var data = sources.Select(IncomeSourceMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<IncomeSourceResponse>>.Ok(data);
    }
}
