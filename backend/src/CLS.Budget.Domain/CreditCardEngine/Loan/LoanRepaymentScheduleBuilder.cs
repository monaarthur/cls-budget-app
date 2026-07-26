namespace CLS.Budget.Domain.CreditCardEngine.Loan;

/// <summary>
/// Builds a standalone loan amortization schedule and interest totals by loan type.
/// </summary>
public static class LoanRepaymentScheduleBuilder
{
    public static string DisplayName(LoanType type) => type switch
    {
        LoanType.Personal => "Personal loan",
        LoanType.HomeEquity => "Home equity / second mortgage",
        LoanType.Heloc => "HELOC",
        LoanType.Retirement401k => "401(k) loan",
        LoanType.Family => "Family / private loan",
        _ => "Loan"
    };

    public static LoanScheduleResult Build(LoanScheduleRequest request)
    {
        var errors = new List<string>();
        var amount = CreditCardMath.RoundMoney(request.Amount);
        if (amount <= 0)
        {
            errors.Add("Loan amount must be greater than zero.");
        }

        var apr = request.AnnualPercentageRate;
        if (apr < 0)
        {
            errors.Add("Loan APR must be zero or greater.");
        }

        return request.LoanType switch
        {
            LoanType.Family => BuildFamily(amount, apr, request.FixedMonthlyPayment, errors),
            LoanType.Heloc => BuildHeloc(
                amount,
                apr,
                request.TermMonths,
                request.InterestOnlyMonths,
                errors),
            LoanType.Personal or LoanType.HomeEquity or LoanType.Retirement401k =>
                BuildAmortizing(request.LoanType, amount, apr, request.TermMonths, errors),
            _ => Invalid(errors, "Unsupported loan type.")
        };
    }

    /// <summary>
    /// Monthly payment that must be reserved in a combined payoff budget
    /// (uses the higher HELOC amortizing payment when applicable).
    /// </summary>
    public static decimal RequiredMonthlyBudget(LoanScheduleResult schedule)
    {
        if (!schedule.IsValid)
        {
            return 0m;
        }

        return schedule.Phase2MonthlyPayment is > 0
            ? schedule.Phase2MonthlyPayment.Value
            : schedule.MonthlyPayment;
    }

    public static decimal CalculateAmortizingPayment(decimal principal, decimal aprPercent, int termMonths)
    {
        if (principal <= 0 || termMonths <= 0)
        {
            return 0m;
        }

        var r = CreditCardMath.MonthlyRate(aprPercent);
        if (r == 0m)
        {
            return CreditCardMath.RoundMoney(principal / termMonths);
        }

        var factor = Pow(1m + r, termMonths);
        var payment = principal * r * factor / (factor - 1m);
        return CreditCardMath.RoundMoney(payment);
    }

    private static LoanScheduleResult BuildAmortizing(
        LoanType type,
        decimal amount,
        decimal apr,
        int? termMonths,
        List<string> errors)
    {
        if (termMonths is null or < 1)
        {
            errors.Add("Term (months) is required for this loan type.");
        }

        if (errors.Count > 0 || amount <= 0 || termMonths is null or < 1)
        {
            return Invalid(errors);
        }

        var payment = CalculateAmortizingPayment(amount, apr, termMonths.Value);
        if (payment <= 0)
        {
            errors.Add("Could not calculate a valid monthly payment.");
            return Invalid(errors);
        }

        var schedule = RunFixedPaymentSchedule(amount, apr, payment, termMonths.Value + 2);
        return new LoanScheduleResult(
            IsValid: true,
            Errors: [],
            LoanTypeDisplayName: DisplayName(type),
            MonthlyPayment: payment,
            Phase2MonthlyPayment: null,
            MonthsToPayoff: schedule.Count,
            TotalInterest: CreditCardMath.RoundMoney(schedule.Sum(s => s.Interest)),
            TotalPaid: CreditCardMath.RoundMoney(schedule.Sum(s => s.Payment)),
            Schedule: schedule);
    }

    private static LoanScheduleResult BuildHeloc(
        decimal amount,
        decimal apr,
        int? termMonths,
        int? interestOnlyMonths,
        List<string> errors)
    {
        if (termMonths is null or < 1)
        {
            errors.Add("Total term (months) is required for a HELOC.");
        }

        var ioMonths = interestOnlyMonths ?? 0;
        if (ioMonths < 0)
        {
            errors.Add("Interest-only months cannot be negative.");
        }

        if (termMonths is not null && ioMonths >= termMonths.Value)
        {
            errors.Add("Interest-only months must be less than the total term.");
        }

        if (errors.Count > 0 || amount <= 0 || termMonths is null or < 1)
        {
            return Invalid(errors);
        }

        var amortMonths = termMonths.Value - ioMonths;
        var phase2Payment = CalculateAmortizingPayment(amount, apr, amortMonths);
        var monthlyRate = CreditCardMath.MonthlyRate(apr);
        var rows = new List<LoanScheduleMonth>();
        var balance = amount;
        var month = 0;

        for (var i = 0; i < ioMonths && balance > 0.005m; i++)
        {
            month++;
            var interest = CreditCardMath.RoundMoney(balance * monthlyRate);
            var payment = interest;
            rows.Add(new LoanScheduleMonth(
                month,
                payment,
                interest,
                0m,
                balance));
        }

        if (phase2Payment <= 0 && balance > 0.005m)
        {
            errors.Add("Could not calculate a valid HELOC amortizing payment.");
            return Invalid(errors);
        }

        var firstPayment = ioMonths > 0
            ? CreditCardMath.RoundMoney(amount * monthlyRate)
            : phase2Payment;

        while (balance > 0.005m && month < CreditCardMath.MaxPayoffMonths)
        {
            month++;
            var interest = CreditCardMath.RoundMoney(balance * monthlyRate);
            var payment = CreditCardMath.RoundMoney(Math.Min(balance + interest, phase2Payment));
            if (payment <= interest && balance > payment)
            {
                // Ensure progress if payment too small (shouldn't happen with valid amortization).
                payment = CreditCardMath.RoundMoney(Math.Min(balance + interest, Math.Max(phase2Payment, interest + 0.01m)));
            }

            var principal = CreditCardMath.RoundMoney(Math.Min(balance, payment - interest));
            if (principal < 0)
            {
                principal = 0;
                payment = interest;
            }

            balance = CreditCardMath.RoundMoney(balance - principal);
            if (balance < 0.005m)
            {
                balance = 0m;
            }

            rows.Add(new LoanScheduleMonth(month, payment, interest, principal, balance));
        }

        return new LoanScheduleResult(
            IsValid: true,
            Errors: [],
            LoanTypeDisplayName: DisplayName(LoanType.Heloc),
            MonthlyPayment: firstPayment,
            Phase2MonthlyPayment: phase2Payment,
            MonthsToPayoff: rows.Count,
            TotalInterest: CreditCardMath.RoundMoney(rows.Sum(s => s.Interest)),
            TotalPaid: CreditCardMath.RoundMoney(rows.Sum(s => s.Payment)),
            Schedule: rows);
    }

    private static LoanScheduleResult BuildFamily(
        decimal amount,
        decimal apr,
        decimal? fixedPayment,
        List<string> errors)
    {
        if (fixedPayment is null or <= 0)
        {
            errors.Add("A fixed monthly payment is required for a family / private loan.");
        }

        if (errors.Count > 0 || amount <= 0 || fixedPayment is null or <= 0)
        {
            return Invalid(errors);
        }

        var monthlyRate = CreditCardMath.MonthlyRate(apr);
        var firstInterest = CreditCardMath.RoundMoney(amount * monthlyRate);
        if (fixedPayment.Value + 0.0001m < firstInterest && firstInterest > 0)
        {
            errors.Add(
                $"Fixed monthly payment ({fixedPayment.Value:C}) is below the first month's interest ({firstInterest:C}); the loan would never pay down.");
            return Invalid(errors);
        }

        var schedule = RunFixedPaymentSchedule(amount, apr, fixedPayment.Value, CreditCardMath.MaxPayoffMonths);
        if (schedule.Count == 0 || schedule[^1].EndingBalance > 0.005m)
        {
            errors.Add("Loan does not pay off within the maximum forecast horizon at this payment.");
            return Invalid(errors);
        }

        return new LoanScheduleResult(
            IsValid: true,
            Errors: [],
            LoanTypeDisplayName: DisplayName(LoanType.Family),
            MonthlyPayment: CreditCardMath.RoundMoney(fixedPayment.Value),
            Phase2MonthlyPayment: null,
            MonthsToPayoff: schedule.Count,
            TotalInterest: CreditCardMath.RoundMoney(schedule.Sum(s => s.Interest)),
            TotalPaid: CreditCardMath.RoundMoney(schedule.Sum(s => s.Payment)),
            Schedule: schedule);
    }

    private static List<LoanScheduleMonth> RunFixedPaymentSchedule(
        decimal amount,
        decimal apr,
        decimal fixedPayment,
        int maxMonths)
    {
        var rows = new List<LoanScheduleMonth>();
        var balance = amount;
        var monthlyRate = CreditCardMath.MonthlyRate(apr);
        var month = 0;

        while (balance > 0.005m && month < maxMonths)
        {
            month++;
            var interest = CreditCardMath.RoundMoney(balance * monthlyRate);
            var payment = CreditCardMath.RoundMoney(Math.Min(balance + interest, fixedPayment));
            var principal = CreditCardMath.RoundMoney(Math.Min(balance, Math.Max(0m, payment - interest)));
            balance = CreditCardMath.RoundMoney(balance - principal);
            if (balance < 0.005m)
            {
                balance = 0m;
            }

            rows.Add(new LoanScheduleMonth(month, payment, interest, principal, balance));
        }

        return rows;
    }

    private static LoanScheduleResult Invalid(List<string> errors, string? extra = null)
    {
        if (extra is not null)
        {
            errors.Add(extra);
        }

        return new LoanScheduleResult(
            IsValid: false,
            Errors: errors,
            LoanTypeDisplayName: "Loan",
            MonthlyPayment: 0,
            Phase2MonthlyPayment: null,
            MonthsToPayoff: 0,
            TotalInterest: 0,
            TotalPaid: 0,
            Schedule: []);
    }

    private static decimal Pow(decimal value, int exponent)
    {
        decimal result = 1m;
        for (var i = 0; i < exponent; i++)
        {
            result *= value;
        }

        return result;
    }
}
