
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public sealed class Upgrade
{
    public int UpgradeId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}

