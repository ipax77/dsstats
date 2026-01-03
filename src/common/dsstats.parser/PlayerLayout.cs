namespace dsstats.parser;

internal class PlayerLayout
{
    public Pos South { get; set; } = Pos.Zero;
    public Pos West { get; set; } = Pos.Zero;
    public Pos North { get; set; } = Pos.Zero;
    public Pos East { get; set; } = Pos.Zero;
    public bool IsReady()
    {
        return South != Pos.Zero && West != Pos.Zero && North != Pos.Zero && East != Pos.Zero;
    }
}