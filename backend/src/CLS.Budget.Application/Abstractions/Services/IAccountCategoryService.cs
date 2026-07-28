using CLS.Budget.Application.AccountCategories.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IAccountCategoryService
{
    Task<ApiResponse<IReadOnlyList<AccountCategoryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AccountCategoryResponse>> CreateCategoryAsync(
        CreateAccountCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AccountSubCategoryResponse>> CreateSubCategoryAsync(
        CreateAccountSubCategoryRequest request,
        CancellationToken cancellationToken = default);
}
