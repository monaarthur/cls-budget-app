using CLS.Budget.Application.BudgetTemplates.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IBudgetTemplateService
{
    Task<ApiResponse<IReadOnlyList<BudgetTemplateResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
