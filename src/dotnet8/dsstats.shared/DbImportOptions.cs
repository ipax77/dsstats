
namespace dsstats.shared;

public record DbImportOptions
{
    public string ImportConnectionString { get; set; } = string.Empty;
    public bool IsSqlite { get; set; }
}

public record BlizzardAPIOptions
{
    public string ClientName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}