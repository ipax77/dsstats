using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public sealed class UploadJob
{
    public int UploadJobId { get; set; }
    public int[] PlayerIds { get; set; } = [];
    [MaxLength(10)]
    public string? Version { get; set; }
    public string BlobFilePath { get; set; } = string.Empty;
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    [Precision(0)]
    public DateTime? FinishedAt { get; set; }
    [MaxLength(200)]
    public string? Error { get; set; } = string.Empty;
}

public sealed class ReplayUploadJob
{
    public int ReplayUploadJobId { get; set; }
    public Guid Guid { get; set; }
    public string BlobFilePath { get; set; } = string.Empty;
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    [Precision(0)]
    public DateTime? FinishedAt { get; set; }
    [MaxLength(200)]
    public string? Error { get; set; } = string.Empty;
}

