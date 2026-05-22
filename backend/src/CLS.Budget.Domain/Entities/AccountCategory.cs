namespace CLS.Budget.Domain.Entities;

public class AccountCategory
{
    public int AccountCategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
