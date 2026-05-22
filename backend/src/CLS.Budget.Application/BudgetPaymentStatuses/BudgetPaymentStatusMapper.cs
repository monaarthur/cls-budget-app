using CLS.Budget.Application.BudgetPaymentStatuses.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.BudgetPaymentStatuses;

internal static class BudgetPaymentStatusMapper
{
    public static BudgetPaymentStatusResponse ToResponse(BudgetPaymentStatus status) => new()
    {
        BudgetPaymentStatusId = status.BudgetPaymentStatusId,
        Name = status.Name,
        Description = status.Description
    };
}
