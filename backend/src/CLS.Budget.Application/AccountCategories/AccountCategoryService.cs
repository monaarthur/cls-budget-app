using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.AccountCategories.Dtos;
using CLS.Budget.Application.Common;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.AccountCategories;

public sealed class AccountCategoryService(IAccountCategoryRepository accountCategoryRepository)
    : IAccountCategoryService
{
    public async Task<ApiResponse<IReadOnlyList<AccountCategoryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await accountCategoryRepository.GetAllForTenantAsync(cancellationToken);
        var data = categories.Select(AccountCategoryMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<AccountCategoryResponse>>.Ok(data);
    }

    public async Task<ApiResponse<AccountCategoryResponse>> CreateCategoryAsync(
        CreateAccountCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name.Trim();
        var existing = await accountCategoryRepository.FindByNameForTenantAsync(
            trimmedName,
            cancellationToken);
        if (existing is not null)
        {
            return ApiResponse<AccountCategoryResponse>.Ok(
                AccountCategoryMapper.ToResponse(existing));
        }

        var created = await accountCategoryRepository.AddCategoryAsync(
            new AccountCategory
            {
                Name = trimmedName,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim()
            },
            cancellationToken);

        return ApiResponse<AccountCategoryResponse>.Ok(
            AccountCategoryMapper.ToResponse(created));
    }

    public async Task<ApiResponse<AccountSubCategoryResponse>> CreateSubCategoryAsync(
        CreateAccountSubCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var parent = await accountCategoryRepository.GetByIdForTenantAsync(
            request.AccountCategoryId,
            cancellationToken);
        if (parent is null)
        {
            return ApiResponse<AccountSubCategoryResponse>.Fail("Category was not found.");
        }

        var trimmedName = request.Name.Trim();
        var existing = await accountCategoryRepository.FindSubCategoryByNameAsync(
            request.AccountCategoryId,
            trimmedName,
            cancellationToken);
        if (existing is not null)
        {
            return ApiResponse<AccountSubCategoryResponse>.Ok(
                AccountCategoryMapper.ToSubCategoryResponse(existing));
        }

        var created = await accountCategoryRepository.AddSubCategoryAsync(
            new AccountSubCategory
            {
                AccountCategoryId = request.AccountCategoryId,
                Name = trimmedName,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim()
            },
            cancellationToken);

        return ApiResponse<AccountSubCategoryResponse>.Ok(
            AccountCategoryMapper.ToSubCategoryResponse(created));
    }
}
