using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.IncomeSources.Dtos;
using CLS.Budget.Domain.Entities;

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

    public async Task<ApiResponse<IncomeSourceResponse>> CreateAsync(
        CreateIncomeSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name.Trim();
        var existing = await incomeSourceRepository.GetByNameAsync(trimmedName, cancellationToken);
        if (existing is not null)
        {
            return ApiResponse<IncomeSourceResponse>.Ok(IncomeSourceMapper.ToResponse(existing));
        }

        var created = await incomeSourceRepository.AddAsync(
            new IncomeSource
            {
                Name = trimmedName,
                IsActive = true
            },
            cancellationToken);

        return ApiResponse<IncomeSourceResponse>.Ok(IncomeSourceMapper.ToResponse(created));
    }
}
