namespace sc2dsstats.shared
{
    public class TimelineRequest : DsRequest
    {
        public int Step { get; set; } = 500;
        public int smaK { get; set; } = 6;
    }
}
