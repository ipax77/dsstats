
namespace dsstats.shared8;

public record DbImportOptions8
{
    public string ImportConnectionString { get; set; } = string.Empty;
    public bool IsSqlite { get; set; }
    public string MySqlImportDir { get; set; } = string.Empty;
}