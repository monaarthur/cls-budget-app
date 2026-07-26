namespace CLS.Budget.Domain.CreditCardEngine.Interest;

public interface IInterestCalculationEngine
{
    InterestCalculationResult Calculate(InterestCalculationRequest request);
}
