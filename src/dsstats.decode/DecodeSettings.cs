
namespace dsstats.decode;

public record DecodeSettings
{
    public ReplayFolders ReplayFolders { get; set; } = new();
    public int Threads { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public string RawCallbackUrl { get; set; } = string.Empty;
}

public record ReplayFolders
{
    public string ToDo { get; set; } = string.Empty;
    public string ToDoRaw { get; set; } = string.Empty;
    public string Done { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}