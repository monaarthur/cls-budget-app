namespace CLS.Budget.Application.AccountCategories.Dtos;

public sealed class AccountCategoryResponse
{
    public int AccountCategoryId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public IReadOnlyList<AccountSubCategoryResponse> SubCategories { get; init; } = [];
}

public sealed class AccountSubCategoryResponse
{
    public int AccountSubCategoryId { get; init; }
    public int AccountCategoryId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}

public sealed class CreateAccountCategoryRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}

public sealed class CreateAccountSubCategoryRequest
{
    public int AccountCategoryId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}
