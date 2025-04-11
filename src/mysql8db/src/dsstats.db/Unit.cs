
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public sealed class Unit
{
    public int UnitId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}

