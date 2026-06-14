using CLS.Budget.Application.IncomeSources.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.IncomeSources;

internal static class IncomeSourceMapper
{
    public static IncomeSourceResponse ToResponse(IncomeSource source) => new()
    {
        IncomeSourceId = source.IncomeSourceId,
        Name = source.Name,
        IsActive = source.IsActive
    };
}
