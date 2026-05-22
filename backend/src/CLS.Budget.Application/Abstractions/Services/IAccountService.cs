using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IAccountService
{
    Task<ApiResponse<IReadOnlyList<AccountResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<AccountResponse>> GetByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AccountResponse>> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AccountResponse>> UpdateAsync(int accountId, UpdateAccountRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(int accountId, CancellationToken cancellationToken = default);
}
