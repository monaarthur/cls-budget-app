using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Accounts;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.CreditCards;

public sealed class CreditCardService(IAccountRepository accountRepository) : ICreditCardService
{
    public async Task<ApiResponse<IReadOnlyList<AccountResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await accountRepository.GetByCategoryAsync(CreditCardCategory.CategoryId, cancellationToken);
        var data = accounts.Select(AccountMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<AccountResponse>>.Ok(data);
    }

    public async Task<ApiResponse<AccountResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAndCategoryAsync(id, CreditCardCategory.CategoryId, cancellationToken);
        if (account is null)
        {
            return ApiResponse<AccountResponse>.Fail($"Credit card with id {id} was not found.");
        }

        return ApiResponse<AccountResponse>.Ok(AccountMapper.ToResponse(account));
    }

    public async Task<ApiResponse<AccountResponse>> CreateAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = AccountMapper.ToEntity(request);
        account.AccountCategoryId = CreditCardCategory.CategoryId;
        account.IsCreditCard = request.IsCreditCard ?? true;
        var created = await accountRepository.AddAsync(account, cancellationToken);
        return ApiResponse<AccountResponse>.Ok(AccountMapper.ToResponse(created));
    }

    public async Task<ApiResponse<AccountResponse>> UpdateAsync(
        int id,
        UpdateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAndCategoryAsync(id, CreditCardCategory.CategoryId, cancellationToken);
        if (account is null)
        {
            return ApiResponse<AccountResponse>.Fail($"Credit card with id {id} was not found.");
        }

        AccountMapper.ApplyUpdate(account, request);
        account.AccountCategoryId = CreditCardCategory.CategoryId;
        account.IsCreditCard = request.IsCreditCard ?? true;
        await accountRepository.UpdateAsync(account, cancellationToken);
        return ApiResponse<AccountResponse>.Ok(AccountMapper.ToResponse(account));
    }

    public async Task<ApiResponse<object>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAndCategoryAsync(id, CreditCardCategory.CategoryId, cancellationToken);
        if (account is null)
        {
            return ApiResponse<object>.Fail($"Credit card with id {id} was not found.");
        }

        await accountRepository.DeleteAsync(account, cancellationToken);
        return ApiResponse<object>.Ok(new { });
    }
}
