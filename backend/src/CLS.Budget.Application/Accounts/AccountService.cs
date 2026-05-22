using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Accounts;

public sealed class AccountService(IAccountRepository accountRepository) : IAccountService
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
        var account = AccountMapper.ToEntity(request);
        var created = await accountRepository.AddAsync(account, cancellationToken);
        return ApiResponse<AccountResponse>.Ok(AccountMapper.ToResponse(created));
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

        AccountMapper.ApplyUpdate(account, request);
        await accountRepository.UpdateAsync(account, cancellationToken);
        return ApiResponse<AccountResponse>.Ok(AccountMapper.ToResponse(account));
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
}
