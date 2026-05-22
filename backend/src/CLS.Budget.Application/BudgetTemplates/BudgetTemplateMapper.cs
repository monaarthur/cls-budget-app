using CLS.Budget.Application.Budgets;
using CLS.Budget.Application.BudgetTemplates.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.BudgetTemplates;

internal static class BudgetTemplateMapper
{
    public static BudgetTemplateResponse ToResponse(BudgetTemplate template) => new()
    {
        BudgetTemplateId = template.BudgetTemplateId,
        Name = template.Name,
        Description = template.Description,
        AccountIds = BudgetTemplateAccountIdsParser.Parse(template.AccountIds)
    };
}
