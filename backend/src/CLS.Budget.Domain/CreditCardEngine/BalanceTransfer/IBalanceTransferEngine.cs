namespace CLS.Budget.Domain.CreditCardEngine.BalanceTransfer;

public interface IBalanceTransferEngine
{
    BalanceTransferAnalysisResult Analyze(BalanceTransferAnalysisRequest request);
}
