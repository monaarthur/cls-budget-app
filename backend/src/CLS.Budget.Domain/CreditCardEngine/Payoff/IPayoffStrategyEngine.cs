namespace CLS.Budget.Domain.CreditCardEngine.Payoff;

public interface IPayoffStrategyEngine
{
    PayoffPlanResult GeneratePlan(PayoffPlanRequest request);
}
