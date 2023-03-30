namespace pax.dsstats.shared;

public record ImportRequest
{
    public List<string> Replayblobs { get; set; } = new();
}

public record ImportResult
{

}