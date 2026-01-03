namespace dsstats.shared;

public sealed class HostOptions
{
    public HostAppKind Kind { get; set; }
}

public enum HostAppKind
{
    BlazorServer,
    BlazorWasmPwa,
    MauiBlazor
}
