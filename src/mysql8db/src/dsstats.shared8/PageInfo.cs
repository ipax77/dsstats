namespace dsstats.shared8;

public sealed class PageInfo
{
    public int TotalSize { get; set; }
    public int Page { get; set; }
    public int PageRequest { get; set; }
    public int PageSize { get; set; } = 100_000;
    public int MaxPages => TotalSize == 0 ? 0 : TotalSize / PageSize;
    public bool CanMoveLeft => Page != 0;
    public bool CanMoveRight => Page < MaxPages;
}
