namespace dsstats.builder;

class Program
{
    static void Main(string[] args)
    {
        Thread.Sleep(1000);
        var events = DsBuilder.BuildArmy("Zerg", 2560, 1440, false);
        BuildPlayer.ReplayInput(events);
    }
}
