namespace dsstats.builder;

class Program
{
    static void Main(string[] args)
    {
        Thread.Sleep(1000);
        var events = DsBuilder.BuildArmy(shared.Commander.Zerg, 2560, 1440, true);
        BuildPlayer.ReplayInput(events);
    }
}
