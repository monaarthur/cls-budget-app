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
        Phone = account.Phone,
        Email = account.Email,
        Url = account.Url,
        Username = account.Username,
        Notes = account.Notes,
        IsPaidOff = account.IsPaidOff,
        PaidOffDate = account.PaidOffDate,
        IsCreditCard = account.IsCreditCard,
        AccountCategoryId = account.AccountCategoryId
    };

    public static Account ToEntity(CreateAccountRequest request) => new()
    {
        Name = request.Name,
        Number = request.Number,
        Description = request.Description,
        Balance = request.Balance,
        Limit = request.Limit,
        AccountOpenDate = request.AccountOpenDate,
        MonthlyPayment = request.MonthlyPayment,
        PaymentDay = request.PaymentDay,
        Phone = request.Phone,
        Email = request.Email,
        Url = request.Url,
        Username = request.Username,
        Password = request.Password,
        Notes = request.Notes,
        IsPaidOff = request.IsPaidOff,
        PaidOffDate = request.PaidOffDate,
        IsCreditCard = request.IsCreditCard,
        AccountCategoryId = request.AccountCategoryId
    };

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
    }
}
