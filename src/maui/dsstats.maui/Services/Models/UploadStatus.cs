namespace dsstats.maui.Services.Models;

public enum UploadStatus
{
    None = 0,
    Forbidden = 1,
    Uploading = 2,
    Success = 3,
    Failed = 4,
};

public enum ImportStatus
{
    Idle,
    Running,
    Completed,
    Cancelled,
    Failed
}