using CLS.Budget.Application.Budgets.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Budgets;

internal static class BudgetMapper
{
    public static BudgetResponse ToResponse(BudgetModel budget) => new()
    {
        BudgetId = budget.BudgetId,
        Name = budget.Name,
        StartPeriod = budget.StartPeriod,
        EndPeriod = budget.EndPeriod,
        BudgetTemplateId = budget.BudgetTemplateId,
        Notes = budget.Notes,
        AccountIds = BudgetTemplateAccountIdsParser.Parse(budget.AccountIds),
        PayScheduleId = budget.PayScheduleId
    };

    public static BudgetModel ToEntity(CreateBudgetRequest request) => new()
    {
        Name = request.Name,
        StartPeriod = request.StartPeriod,
        EndPeriod = request.EndPeriod,
        BudgetTemplateId = request.BudgetTemplateId,
        Notes = request.Notes,
        PayScheduleId = request.PayScheduleId
    };

    public static BudgetModel ToCopiedEntity(BudgetModel source, CopyBudgetRequest request, int budgetTemplateId) => new()
    {
        Name = request.Name,
        StartPeriod = request.StartPeriod,
        EndPeriod = request.EndPeriod,
        BudgetTemplateId = budgetTemplateId,
        Notes = source.Notes,
        AccountIds = source.AccountIds,
        PayScheduleId = source.PayScheduleId
    };

    public static void ApplyUpdate(BudgetModel budget, UpdateBudgetRequest request)
    {
        budget.Name = request.Name;
        budget.StartPeriod = request.StartPeriod;
        budget.EndPeriod = request.EndPeriod;
        budget.BudgetTemplateId = request.BudgetTemplateId;
        budget.Notes = request.Notes;
        budget.PayScheduleId = request.PayScheduleId;
    }
}
