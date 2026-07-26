namespace CLS.Budget.Domain.CreditCardEngine.BalanceTransfer;

public sealed class BalanceTransferEngine : IBalanceTransferEngine
{
    public BalanceTransferAnalysisResult Analyze(BalanceTransferAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var warnings = new List<string>();

        if (request.TransferAmount <= 0
            || request.PromotionalPeriodMonths <= 0
            || request.PlannedMonthlyPayment < 0
            || request.AvailableTransferLimit < 0
            || request.CurrentAnnualPercentageRate < 0
            || request.PromotionalAnnualPercentageRate < 0
            || request.NewRegularAnnualPercentageRate < 0
            || request.TransferFeePercentage < 0
            || request.TransferFeeFlatAmount < 0)
        {
            return Invalid(
                request,
                BalanceTransferRecommendation.InsufficientInformation,
                "Provide a positive transfer amount, promotional period, and non-negative rates, fees, and payment.",
                warnings);
        }

        var appliedAmount = CreditCardMath.RoundMoney(
            Math.Min(request.TransferAmount, request.AvailableTransferLimit));
        if (appliedAmount <= 0)
        {
            warnings.Add("Available transfer limit is zero, so no transfer can be applied.");
            return Invalid(
                request,
                BalanceTransferRecommendation.InsufficientInformation,
                "The available transfer limit is too low to move any balance.",
                warnings);
        }

        if (appliedAmount < request.TransferAmount)
        {
            warnings.Add(
                $"Transfer amount was capped at the available limit of {appliedAmount:C}.");
        }

        var percentFee = CreditCardMath.RoundMoney(
            appliedAmount * (request.TransferFeePercentage / 100m));
        var totalFee = CreditCardMath.RoundMoney(percentFee + request.TransferFeeFlatAmount);
        var startingWithTransfer = request.IncludeFeeInTransferredBalance
            ? CreditCardMath.RoundMoney(appliedAmount + totalFee)
            : appliedAmount;

        var promoMonths = request.PromotionalPeriodMonths;
        var promoEnd = request.StartDate.AddMonths(promoMonths);

        var paymentToClear = PaymentToClearInMonths(
            startingWithTransfer,
            request.PromotionalAnnualPercentageRate,
            promoMonths);

        var without = Simulate(
            startingBalance: appliedAmount,
            standardApr: request.CurrentAnnualPercentageRate,
            promotionalApr: null,
            promotionalExpiration: null,
            monthlyPayment: request.PlannedMonthlyPayment,
            startDate: request.StartDate,
            maxMonths: promoMonths);

        var with = Simulate(
            startingBalance: startingWithTransfer,
            standardApr: request.NewRegularAnnualPercentageRate,
            promotionalApr: request.PromotionalAnnualPercentageRate,
            promotionalExpiration: promoEnd,
            monthlyPayment: request.PlannedMonthlyPayment,
            startDate: request.StartDate,
            maxMonths: promoMonths);

        var interestWithout = without.TotalInterest;
        var interestWith = with.TotalInterest;
        var netSavings = CreditCardMath.RoundMoney(interestWithout - interestWith - totalFee);
        var balanceAtPromoEnd = with.EndingBalance;

        var breakEven = FindBreakEvenMonth(
            appliedAmount,
            startingWithTransfer,
            request,
            promoEnd,
            totalFee,
            promoMonths);

        if (balanceAtPromoEnd > 0)
        {
            warnings.Add(
                "The planned payment will not clear the transferred balance before the promotional period ends.");
        }

        if (totalFee > 0 && interestWithout - interestWith < totalFee)
        {
            warnings.Add("The transfer fee exceeds the estimated interest savings over the promotional period.");
        }

        if (request.PlannedMonthlyPayment <= 0)
        {
            warnings.Add("A planned monthly payment of zero will not reduce the transferred balance.");
        }

        var (recommendation, explanation) = Classify(
            netSavings,
            totalFee,
            interestWithout,
            interestWith,
            balanceAtPromoEnd,
            paymentToClear,
            request.PlannedMonthlyPayment);

        return new BalanceTransferAnalysisResult(
            RequestedTransferAmount: CreditCardMath.RoundMoney(request.TransferAmount),
            AppliedTransferAmount: appliedAmount,
            TotalTransferFee: totalFee,
            StartingBalanceWithTransfer: startingWithTransfer,
            InterestWithoutTransfer: interestWithout,
            InterestWithTransfer: interestWith,
            NetSavings: netSavings,
            BreakEvenMonth: breakEven,
            BalanceRemainingWhenPromotionEnds: balanceAtPromoEnd,
            PaymentNeededToClearBeforePromotionEnds: paymentToClear,
            MonthsCompared: promoMonths,
            Recommendation: recommendation,
            Explanation: explanation,
            Warnings: warnings,
            IsValid: true);
    }

    private static BalanceTransferAnalysisResult Invalid(
        BalanceTransferAnalysisRequest request,
        BalanceTransferRecommendation recommendation,
        string explanation,
        List<string> warnings) =>
        new(
            RequestedTransferAmount: CreditCardMath.RoundMoney(Math.Max(0m, request.TransferAmount)),
            AppliedTransferAmount: 0,
            TotalTransferFee: 0,
            StartingBalanceWithTransfer: 0,
            InterestWithoutTransfer: 0,
            InterestWithTransfer: 0,
            NetSavings: 0,
            BreakEvenMonth: null,
            BalanceRemainingWhenPromotionEnds: 0,
            PaymentNeededToClearBeforePromotionEnds: 0,
            MonthsCompared: Math.Max(0, request.PromotionalPeriodMonths),
            Recommendation: recommendation,
            Explanation: explanation,
            Warnings: warnings,
            IsValid: false);

    private static (BalanceTransferRecommendation, string) Classify(
        decimal netSavings,
        decimal totalFee,
        decimal interestWithout,
        decimal interestWith,
        decimal balanceAtPromoEnd,
        decimal paymentToClear,
        decimal plannedPayment)
    {
        if (netSavings > 0 && balanceAtPromoEnd <= 0)
        {
            return (
                BalanceTransferRecommendation.Recommended,
                $"This transfer is estimated to save {netSavings:C} versus keeping the balance at the current APR, and the planned payment clears the balance during the promotional period.");
        }

        if (netSavings > 0 && balanceAtPromoEnd > 0)
        {
            return (
                BalanceTransferRecommendation.PotentiallyBeneficial,
                $"This transfer may save about {netSavings:C} in interest over the promotional window, but the planned payment leaves {balanceAtPromoEnd:C} when the promo ends. Paying about {paymentToClear:C}/month would clear it in time.");
        }

        if (netSavings <= 0 && interestWithout > interestWith && totalFee > 0)
        {
            return (
                BalanceTransferRecommendation.NotRecommended,
                $"Interest would be lower with the transfer, but the {totalFee:C} fee outweighs those savings (net {netSavings:C}).");
        }

        if (plannedPayment < paymentToClear && paymentToClear > 0)
        {
            return (
                BalanceTransferRecommendation.NotRecommended,
                $"At the planned payment, this transfer is not estimated to improve outcomes (net {netSavings:C}). Raising the payment to about {paymentToClear:C}/month would clear the balance before the promo ends.");
        }

        return (
            BalanceTransferRecommendation.NotRecommended,
            $"This transfer is not estimated to improve outcomes over the promotional period (net {netSavings:C}).");
    }

    private static int? FindBreakEvenMonth(
        decimal amountWithout,
        decimal amountWith,
        BalanceTransferAnalysisRequest request,
        DateOnly promoEnd,
        decimal totalFee,
        int promoMonths)
    {
        for (var month = 1; month <= promoMonths; month++)
        {
            var without = Simulate(
                amountWithout,
                request.CurrentAnnualPercentageRate,
                null,
                null,
                request.PlannedMonthlyPayment,
                request.StartDate,
                month);
            var with = Simulate(
                amountWith,
                request.NewRegularAnnualPercentageRate,
                request.PromotionalAnnualPercentageRate,
                promoEnd,
                request.PlannedMonthlyPayment,
                request.StartDate,
                month);

            var interestSaved = CreditCardMath.RoundMoney(without.TotalInterest - with.TotalInterest);
            if (interestSaved >= totalFee)
            {
                return month;
            }
        }

        return null;
    }

    private static decimal PaymentToClearInMonths(
        decimal principal,
        decimal annualPercentageRate,
        int months)
    {
        if (principal <= 0 || months <= 0)
        {
            return 0m;
        }

        var r = CreditCardMath.MonthlyRate(annualPercentageRate);
        if (r == 0m)
        {
            return CreditCardMath.RoundMoney(principal / months);
        }

        var factor = (decimal)Math.Pow((double)(1m + r), months);
        var payment = principal * r * factor / (factor - 1m);
        return CreditCardMath.RoundMoney(payment);
    }

    private static (decimal TotalInterest, decimal EndingBalance) Simulate(
        decimal startingBalance,
        decimal standardApr,
        decimal? promotionalApr,
        DateOnly? promotionalExpiration,
        decimal monthlyPayment,
        DateOnly startDate,
        int maxMonths)
    {
        var balance = CreditCardMath.RoundMoney(startingBalance);
        var totalInterest = 0m;
        var month = startDate;

        for (var i = 0; i < maxMonths && balance > 0; i++)
        {
            var apr = CreditCardMath.EffectiveApr(
                standardApr,
                promotionalApr,
                promotionalExpiration,
                month);
            var interest = CreditCardMath.RoundMoney(balance * CreditCardMath.MonthlyRate(apr));
            balance = CreditCardMath.RoundMoney(balance + interest);
            var payment = Math.Min(monthlyPayment, balance);
            balance = CreditCardMath.RoundMoney(balance - payment);
            totalInterest = CreditCardMath.RoundMoney(totalInterest + interest);
            month = month.AddMonths(1);

            if (balance <= 0)
            {
                balance = 0;
                break;
            }
        }

        return (totalInterest, balance);
    }
}
