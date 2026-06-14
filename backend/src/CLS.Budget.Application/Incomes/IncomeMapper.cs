using CLS.Budget.Application.Incomes.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Incomes;

internal static class IncomeMapper
{
    public static IncomeResponse ToResponse(BudgetIncome income) => new()
    {
        BudgetIncomeId = income.BudgetIncomeId,
        BudgetId = income.BudgetId,
        IncomeSourceId = income.IncomeSourceId,
        IncomeSourceName = income.IncomeSource?.Name ?? string.Empty,
        Amount = income.Amount,
        ReceivedDate = income.ReceivedDate,
        Notes = income.Notes
    };

    public static BudgetIncome ToEntity(CreateIncomeRequest request) => new()
    {
        BudgetId = request.BudgetId,
        IncomeSourceId = request.IncomeSourceId,
        Amount = request.Amount,
        ReceivedDate = request.ReceivedDate,
        Notes = request.Notes
    };

    public static void ApplyUpdate(BudgetIncome income, UpdateIncomeRequest request)
    {
        income.BudgetId = request.BudgetId;
        income.IncomeSourceId = request.IncomeSourceId;
        income.Amount = request.Amount;
        income.ReceivedDate = request.ReceivedDate;
        income.Notes = request.Notes;
    }
}
