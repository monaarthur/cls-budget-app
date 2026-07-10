namespace CLS.Budget.Api.Admin;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>
    /// Shared secret sent as the X-Admin-Api-Key request header.
    /// </summary>
    public string ApiKey { get; set; } = "";
}
