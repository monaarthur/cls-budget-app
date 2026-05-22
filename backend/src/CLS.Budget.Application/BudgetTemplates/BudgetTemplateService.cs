using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.BudgetTemplates.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.BudgetTemplates;

public sealed class BudgetTemplateService(IBudgetTemplateRepository budgetTemplateRepository)
    : IBudgetTemplateService
{
    public async Task<ApiResponse<IReadOnlyList<BudgetTemplateResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var templates = await budgetTemplateRepository.GetAllAsync(cancellationToken);
        var data = templates.Select(BudgetTemplateMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<BudgetTemplateResponse>>.Ok(data);
    }
}
