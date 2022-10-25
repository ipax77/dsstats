using System.ComponentModel.DataAnnotations;

namespace pax.dsstats.shared;

[Serializable]
public record UploadInfo
{
    public Guid UploadId { get; set; } = Guid.NewGuid();
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Event { get; set; } = "";
    [Required]
    [MinLength(1)]
    public string Team1 { get; set; } = "";
    [Required]
    [MinLength(1)]
    public string Team2 { get; set; } = "";
    [Required]
    [MinLength(1)]
    public string Ban1 { get; set; } = "";
    [Required]
    [MinLength(1)]
    public string Ban2 { get; set; } = "";
    [Required]
    [MinLength(1)]
    public string Round { get; set; } = "";
}

public class UploadResult
{
    public bool Uploaded { get; set; }
    public string? FileName { get; set; }
    public string? StoredFileName { get; set; }
    public int ErrorCode { get; set; }
}