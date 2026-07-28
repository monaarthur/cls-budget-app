using CLS.Budget.Application.AccountCategories.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.AccountCategories;

internal static class AccountCategoryMapper
{
    public static AccountCategoryResponse ToResponse(AccountCategory category) => new()
    {
        AccountCategoryId = category.AccountCategoryId,
        Name = category.Name,
        Description = category.Description,
        IsSystem = category.IsSystem,
        SubCategories = category.SubCategories
            .OrderBy(s => s.Name)
            .Select(ToSubCategoryResponse)
            .ToList()
    };

    public static AccountSubCategoryResponse ToSubCategoryResponse(AccountSubCategory subCategory) => new()
    {
        AccountSubCategoryId = subCategory.AccountSubCategoryId,
        AccountCategoryId = subCategory.AccountCategoryId,
        Name = subCategory.Name,
        Description = subCategory.Description
    };
}
