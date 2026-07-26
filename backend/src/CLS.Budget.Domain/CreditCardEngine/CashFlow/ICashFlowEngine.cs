namespace CLS.Budget.Domain.CreditCardEngine.CashFlow;

public interface ICashFlowEngine
{
    CashFlowResult Calculate(CashFlowRequest request);
}
