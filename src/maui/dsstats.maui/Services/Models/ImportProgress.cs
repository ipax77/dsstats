namespace dsstats.maui.Services.Models;

public record ImportProgress(
    int Total,
    int Discovered,
    int Decoded,
    int Imported,
    int Errors,
    UploadStatus UploadStatus,
    TimeSpan Elapsed,
    ImportStatus ImportStatus,
    string? Message = null);
