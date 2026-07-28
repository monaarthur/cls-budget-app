using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Accounts;

internal static class AccountMapper
{
    public static AccountResponse ToResponse(Account account) => new()
    {
        AccountId = account.AccountId,
        Name = account.Name,
        Number = account.Number,
        Description = account.Description,
        Balance = account.Balance,
        Limit = account.Limit,
        AccountOpenDate = account.AccountOpenDate,
        MonthlyPayment = account.MonthlyPayment,
        PaymentDay = account.PaymentDay,
        GracePeriod = account.GracePeriod,
        GraceDay = account.GraceDay,
        Phone = account.Phone,
        Email = account.Email,
        Url = account.Url,
        Username = account.Username,
        Notes = account.Notes,
        IsPaidOff = account.IsPaidOff,
        PaidOffDate = account.PaidOffDate,
        IsCreditCard = account.IsCreditCard,
        AccountCategoryId = account.AccountCategoryId,
        AccountCategoryName = account.AccountCategory?.Name,
        AccountSubCategoryId = account.AccountSubCategoryId,
        AccountSubCategoryName = account.AccountSubCategory?.Name,
        InterestRate = account.CreditCardDetail?.InterestRate,
        PromotionalAnnualPercentageRate = account.CreditCardDetail?.PromotionalAnnualPercentageRate,
        PromotionalRateExpirationDate = account.CreditCardDetail?.PromotionalRateExpirationDate,
        MinimumPaymentPercentage = account.CreditCardDetail?.MinimumPaymentPercentage,
        MinimumPaymentFloor = account.CreditCardDetail?.MinimumPaymentFloor,
        CashOutInterestRate = account.CreditCardDetail?.CashOutInterestRate,
        CashAdvanceFeePercentage = account.CreditCardDetail?.CashAdvanceFeePercentage,
        IncludeInPayoffAnalysis = account.CreditCardDetail?.IncludeInPayoffAnalysis ?? true
    };

    public static Account ToEntity(CreateAccountRequest request)
    {
        var account = new Account
        {
            Name = request.Name,
            Number = request.Number,
            Description = request.Description,
            Balance = request.Balance,
            Limit = request.Limit,
            AccountOpenDate = request.AccountOpenDate,
            MonthlyPayment = request.MonthlyPayment,
            PaymentDay = request.PaymentDay,
            GracePeriod = request.GracePeriod,
            GraceDay = CalculateGraceDay(request.PaymentDay, request.GracePeriod),
            Phone = request.Phone,
            Email = request.Email,
            Url = request.Url,
            Username = request.Username,
            Password = request.Password,
            Notes = request.Notes,
            IsPaidOff = request.IsPaidOff,
            PaidOffDate = request.PaidOffDate,
            IsCreditCard = request.IsCreditCard,
            AccountCategoryId = request.AccountCategoryId,
            AccountSubCategoryId = request.AccountSubCategoryId
        };

        ApplyCreditCardDetail(account, request);
        return account;
    }

    public static void ApplyUpdate(Account account, UpdateAccountRequest request)
    {
        account.Name = request.Name;
        account.Number = request.Number;
        account.Description = request.Description;
        account.Balance = request.Balance;
        account.Limit = request.Limit;
        account.AccountOpenDate = request.AccountOpenDate;
        account.MonthlyPayment = request.MonthlyPayment;
        account.PaymentDay = request.PaymentDay;
        account.GracePeriod = request.GracePeriod;
        account.GraceDay = CalculateGraceDay(request.PaymentDay, request.GracePeriod);
        account.Phone = request.Phone;
        account.Email = request.Email;
        account.Url = request.Url;
        account.Username = request.Username;
        account.Password = request.Password;
        account.Notes = request.Notes;
        account.IsPaidOff = request.IsPaidOff;
        account.PaidOffDate = request.PaidOffDate;
        account.IsCreditCard = request.IsCreditCard;
        account.AccountCategoryId = request.AccountCategoryId;
        account.AccountSubCategoryId = request.AccountSubCategoryId;
        ApplyCreditCardDetail(account, request);
    }

    private static void ApplyCreditCardDetail(Account account, CreateAccountRequest request) =>
        ApplyCreditCardDetail(
            account,
            request.InterestRate,
            request.PromotionalAnnualPercentageRate,
            request.PromotionalRateExpirationDate,
            request.MinimumPaymentPercentage,
            request.MinimumPaymentFloor,
            request.CashOutInterestRate,
            request.CashAdvanceFeePercentage,
            request.IncludeInPayoffAnalysis,
            request.IsCreditCard);

    private static void ApplyCreditCardDetail(Account account, UpdateAccountRequest request) =>
        ApplyCreditCardDetail(
            account,
            request.InterestRate,
            request.PromotionalAnnualPercentageRate,
            request.PromotionalRateExpirationDate,
            request.MinimumPaymentPercentage,
            request.MinimumPaymentFloor,
            request.CashOutInterestRate,
            request.CashAdvanceFeePercentage,
            request.IncludeInPayoffAnalysis,
            request.IsCreditCard);

    private static void ApplyCreditCardDetail(
        Account account,
        decimal? interestRate,
        decimal? promotionalAnnualPercentageRate,
        DateTime? promotionalRateExpirationDate,
        decimal? minimumPaymentPercentage,
        decimal? minimumPaymentFloor,
        decimal? cashOutInterestRate,
        decimal? cashAdvanceFeePercentage,
        bool includeInPayoffAnalysis,
        bool? isCreditCard)
    {
        var hasDetailFields =
            interestRate is not null
            || promotionalAnnualPercentageRate is not null
            || promotionalRateExpirationDate is not null
            || minimumPaymentPercentage is not null
            || minimumPaymentFloor is not null
            || cashOutInterestRate is not null
            || cashAdvanceFeePercentage is not null
            || !includeInPayoffAnalysis;

        if (account.CreditCardDetail is null)
        {
            if (!hasDetailFields && isCreditCard != true)
            {
                return;
            }

            account.CreditCardDetail = new CreditCardDetail();
        }

        account.CreditCardDetail.InterestRate = interestRate;
        account.CreditCardDetail.PromotionalAnnualPercentageRate = promotionalAnnualPercentageRate;
        account.CreditCardDetail.PromotionalRateExpirationDate = promotionalRateExpirationDate.HasValue
            ? DateTime.SpecifyKind(promotionalRateExpirationDate.Value.ToUniversalTime().Date, DateTimeKind.Utc)
            : null;
        account.CreditCardDetail.MinimumPaymentPercentage = minimumPaymentPercentage;
        account.CreditCardDetail.MinimumPaymentFloor = minimumPaymentFloor;
        account.CreditCardDetail.CashOutInterestRate = cashOutInterestRate;
        account.CreditCardDetail.CashAdvanceFeePercentage = cashAdvanceFeePercentage;
        account.CreditCardDetail.IncludeInPayoffAnalysis = includeInPayoffAnalysis;
    }

    /// <summary>
    /// Day of month when grace ends: PaymentDay + GracePeriod, wrapped into 1–31.
    /// </summary>
    internal static int? CalculateGraceDay(int? paymentDay, int? gracePeriod)
    {
        if (paymentDay is null || gracePeriod is null)
        {
            return null;
        }

        return ((paymentDay.Value - 1 + gracePeriod.Value) % 31) + 1;
    }
}
