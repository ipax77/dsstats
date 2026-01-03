using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public class Unit
{
    public int UnitId { get; set; }
    [MaxLength(40)]
    public string Name { get; set; } = string.Empty;
}
