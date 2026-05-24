using CLS.Budget.Application.PaymentSources.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.PaymentSources;

internal static class PaymentSourceMapper
{
    public static PaymentSourceResponse ToResponse(PaymentSource source) => new()
    {
        PaymentSourceId = source.PaymentSourceId,
        Name = source.Name,
        Description = source.Description
    };
}
