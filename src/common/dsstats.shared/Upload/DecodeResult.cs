namespace dsstats.shared.Upload;


public sealed class DecodeResult
{

}

public sealed class DecodeRequestResult
{
    public bool Success { get; set; }
    public int QueuePosition { get; set; }
    public string? Error { get; set; }
}

public class DecodeFinishedEventArgs : EventArgs
{
    public Guid Guid { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
    public string? Error { get; set; }
}