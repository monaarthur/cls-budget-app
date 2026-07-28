using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Accounts;

public sealed class AccountService(
    IAccountRepository accountRepository,
    IAccountCategoryRepository accountCategoryRepository) : IAccountService
{
    public async Task<ApiResponse<IReadOnlyList<AccountResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await accountRepository.GetAllAsync(cancellationToken);
        var data = accounts.Select(AccountMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<AccountResponse>>.Ok(data);
    }

    public async Task<ApiResponse<AccountResponse>> GetByIdAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return ApiResponse<AccountResponse>.Fail($"Account with id {accountId} was not found.");
        }

        return ApiResponse<AccountResponse>.Ok(AccountMapper.ToResponse(account));
    }

    public async Task<ApiResponse<AccountResponse>> CreateAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoryError = await ValidateCategoryAsync(
            request.AccountCategoryId,
            request.AccountSubCategoryId,
            cancellationToken);
        if (categoryError is not null)
        {
            return ApiResponse<AccountResponse>.Fail(categoryError);
        }

        var account = AccountMapper.ToEntity(request);
        var created = await accountRepository.AddAsync(account, cancellationToken);
        var reloaded = await accountRepository.GetByIdAsync(created.AccountId, cancellationToken);
        return ApiResponse<AccountResponse>.Ok(AccountMapper.ToResponse(reloaded ?? created));
    }

    public async Task<ApiResponse<AccountResponse>> UpdateAsync(
        int accountId,
        UpdateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return ApiResponse<AccountResponse>.Fail($"Account with id {accountId} was not found.");
        }

        var categoryError = await ValidateCategoryAsync(
            request.AccountCategoryId,
            request.AccountSubCategoryId,
            cancellationToken);
        if (categoryError is not null)
        {
            return ApiResponse<AccountResponse>.Fail(categoryError);
        }

        AccountMapper.ApplyUpdate(account, request);
        await accountRepository.UpdateAsync(account, cancellationToken);
        var reloaded = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        return ApiResponse<AccountResponse>.Ok(AccountMapper.ToResponse(reloaded ?? account));
    }

    public async Task<ApiResponse<object>> DeleteAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return ApiResponse<object>.Fail($"Account with id {accountId} was not found.");
        }

        await accountRepository.DeleteAsync(account, cancellationToken);
        return ApiResponse<object>.Ok(new { });
    }

    private async Task<string?> ValidateCategoryAsync(
        int accountCategoryId,
        int? accountSubCategoryId,
        CancellationToken cancellationToken)
    {
        var category = await accountCategoryRepository.GetByIdForTenantAsync(
            accountCategoryId,
            cancellationToken);
        if (category is null)
        {
            return "Account category was not found.";
        }

        if (!accountSubCategoryId.HasValue)
        {
            return null;
        }

        var subCategory = await accountCategoryRepository.GetSubCategoryByIdAsync(
            accountSubCategoryId.Value,
            cancellationToken);
        if (subCategory is null)
        {
            return "Account subcategory was not found.";
        }

        if (subCategory.AccountCategoryId != accountCategoryId)
        {
            return "Subcategory does not belong to the selected category.";
        }

        return null;
    }
}
