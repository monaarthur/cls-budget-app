namespace CLS.Budget.Domain.CreditCardEngine.Utilization;

public interface IUtilizationEngine
{
    UtilizationResult Calculate(IReadOnlyCollection<CreditCardUtilizationInput> cards);
}
