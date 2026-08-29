using CLS.Budget.Application.AutoPayoff.Dtos;
using CLS.Budget.Domain.AutoPayoff;
using CLS.Budget.Domain.Entities;
using System.Text.Json;

namespace CLS.Budget.Application.AutoPayoff;

internal static class AutoPayoffMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public const int DefaultPaidStatusId = 3;

    public static string NormalizeName(string? name) => (name ?? "").Trim();

    public static AutoPayoffConfigResponse DefaultConfig() => new()
    {
        Name = "",
        ExcludeStatusIds = [DefaultPaidStatusId],
        ExcludeCategoryIds = [],
        ExcludeAccountIds = [],
        TargetCategoryIds = [],
        TargetByPayoffDate = false,
        ExtraMonthlyAmount = 0,
    };

    public static IReadOnlyList<int> ParseIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string ToJson(IReadOnlyList<int> ids) =>
        JsonSerializer.Serialize(ids.Distinct().ToList(), JsonOptions);

    public static AutoPayoffConfigResponse ToResponse(AutoPayoffConfig entity) => new()
    {
        AutoPayoffConfigId = entity.AutoPayoffConfigId,
        Name = entity.Name,
        ExcludeStatusIds = ParseIds(entity.ExcludeStatusIdsJson),
        ExcludeCategoryIds = ParseIds(entity.ExcludeCategoryIdsJson),
        ExcludeAccountIds = ParseIds(entity.ExcludeAccountIdsJson),
        TargetCategoryIds = ParseIds(entity.TargetCategoryIdsJson),
        TargetByPayoffDate = entity.TargetByPayoffDate,
        ExtraMonthlyAmount = entity.ExtraMonthlyAmount,
    };

    public static AutoPayoffRules ToRules(AutoPayoffConfigResponse config) => new(
        config.ExcludeStatusIds,
        config.ExcludeCategoryIds,
        config.ExcludeAccountIds,
        config.TargetCategoryIds,
        config.TargetByPayoffDate);

    public static void Apply(AutoPayoffConfig entity, SaveAutoPayoffConfigRequest request, DateTime utcNow)
    {
        entity.Name = NormalizeName(request.Name);
        entity.ExcludeStatusIdsJson = ToJson(request.ExcludeStatusIds);
        entity.ExcludeCategoryIdsJson = ToJson(request.ExcludeCategoryIds);
        entity.ExcludeAccountIdsJson = ToJson(request.ExcludeAccountIds);
        entity.TargetCategoryIdsJson = ToJson(request.TargetCategoryIds);
        entity.TargetByPayoffDate = request.TargetByPayoffDate;
        entity.ExtraMonthlyAmount = Math.Max(0, request.ExtraMonthlyAmount);
        entity.UpdatedOnUtc = utcNow;
    }
}
