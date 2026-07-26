namespace CLS.Budget.Domain.CreditCardEngine.Interest;

public sealed class InterestCalculationEngine : IInterestCalculationEngine
{
    public InterestCalculationResult Calculate(InterestCalculationRequest request)
    {
        if (request.Balance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Balance cannot be negative.");
        }

        if (request.AnnualPercentageRate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "APR cannot be negative.");
        }

        if (request.MonthlyPayment < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Monthly payment cannot be negative.");
        }

        var startApr = CreditCardMath.EffectiveApr(
            request.AnnualPercentageRate,
            request.PromotionalAnnualPercentageRate,
            request.PromotionalRateExpirationDate,
            request.StartDate);

        var dailyInterest = CreditCardMath.RoundMoney(
            request.Balance * CreditCardMath.DailyRate(startApr));
        var estimatedMonthlyInterest = CreditCardMath.RoundMoney(
            request.Balance * CreditCardMath.MonthlyRate(startApr));
        var estimatedAnnualInterest = CreditCardMath.RoundMoney(
            request.Balance * (startApr / 100m));

        if (request.Balance == 0)
        {
            return new InterestCalculationResult(
                DailyInterest: 0,
                EstimatedMonthlyInterest: 0,
                EstimatedAnnualInterest: 0,
                TotalInterestPaid: 0,
                TotalPrincipalPaid: 0,
                RemainingBalance: 0,
                EstimatedPayoffDate: request.StartDate,
                NumberOfPayments: 0,
                NegativeAmortizationDetected: false);
        }

        var balance = request.Balance;
        var totalInterest = 0m;
        var totalPrincipal = 0m;
        var payments = 0;
        var negativeAmortization = false;
        DateOnly? payoffDate = null;
        var month = request.StartDate;

        while (balance > 0 && payments < CreditCardMath.MaxPayoffMonths)
        {
            var apr = CreditCardMath.EffectiveApr(
                request.AnnualPercentageRate,
                request.PromotionalAnnualPercentageRate,
                request.PromotionalRateExpirationDate,
                month);

            var interest = CreditCardMath.RoundMoney(balance * CreditCardMath.MonthlyRate(apr));
            if (interest > 0 && request.MonthlyPayment <= interest)
            {
                negativeAmortization = true;
            }

            balance = CreditCardMath.RoundMoney(balance + interest);
            var payment = Math.Min(request.MonthlyPayment, balance);
            if (payment <= 0 && balance > 0)
            {
                negativeAmortization = true;
                break;
            }

            var principal = CreditCardMath.RoundMoney(payment - Math.Min(interest, payment));
            balance = CreditCardMath.RoundMoney(balance - payment);
            totalInterest = CreditCardMath.RoundMoney(totalInterest + interest);
            totalPrincipal = CreditCardMath.RoundMoney(totalPrincipal + principal);
            payments++;
            month = month.AddMonths(1);

            if (balance <= 0)
            {
                balance = 0;
                payoffDate = month;
                break;
            }
        }

        if (balance > 0)
        {
            payoffDate = null;
        }

        return new InterestCalculationResult(
            DailyInterest: dailyInterest,
            EstimatedMonthlyInterest: estimatedMonthlyInterest,
            EstimatedAnnualInterest: estimatedAnnualInterest,
            TotalInterestPaid: totalInterest,
            TotalPrincipalPaid: totalPrincipal,
            RemainingBalance: balance,
            EstimatedPayoffDate: payoffDate,
            NumberOfPayments: payments,
            NegativeAmortizationDetected: negativeAmortization);
    }
}
