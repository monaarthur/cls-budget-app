namespace CLS.Budget.Application.AutoPayoff.Dtos;

public sealed class SaveAutoPayoffConfigRequest
{
    public int? AutoPayoffConfigId { get; init; }
    public string Name { get; init; } = "";
    public IReadOnlyList<int> ExcludeStatusIds { get; init; } = [];
    public IReadOnlyList<int> ExcludeCategoryIds { get; init; } = [];
    public IReadOnlyList<int> ExcludeAccountIds { get; init; } = [];
    public IReadOnlyList<int> TargetCategoryIds { get; init; } = [];
    public bool TargetByPayoffDate { get; init; }
    public decimal ExtraMonthlyAmount { get; init; }
}

public sealed class AutoPayoffConfigResponse
{
    public int? AutoPayoffConfigId { get; init; }
    public string Name { get; init; } = "";
    public IReadOnlyList<int> ExcludeStatusIds { get; init; } = [];
    public IReadOnlyList<int> ExcludeCategoryIds { get; init; } = [];
    public IReadOnlyList<int> ExcludeAccountIds { get; init; } = [];
    public IReadOnlyList<int> TargetCategoryIds { get; init; } = [];
    public bool TargetByPayoffDate { get; init; }
    public decimal ExtraMonthlyAmount { get; init; }
}

public sealed class AutoPayoffPreviewItemResponse
{
    public int Rank { get; init; }
    public int AccountId { get; init; }
    public string Name { get; init; } = "";
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = "";
    public decimal Balance { get; init; }
    public decimal MonthlyPayment { get; init; }
    public DateTime? TargetDate { get; init; }
    public string Reason { get; init; } = "";
    public decimal ExtraAllocated { get; init; }
}

public sealed class AutoPayoffExcludedItemResponse
{
    public int AccountId { get; init; }
    public string Name { get; init; } = "";
    public string Reason { get; init; } = "";
}

public sealed class AutoPayoffPreviewResponse
{
    public AutoPayoffConfigResponse Config { get; init; } = new();
    public IReadOnlyList<AutoPayoffPreviewItemResponse> Queue { get; init; } = [];
    public IReadOnlyList<AutoPayoffExcludedItemResponse> Excluded { get; init; } = [];
    public decimal ExtraPayment { get; init; }
    public decimal ExtraAllocated { get; init; }
    public decimal ExtraRemaining { get; init; }
}
