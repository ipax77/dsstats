namespace pax.dsstats.shared.Services;

public static class DsUpdates
{
    public static List<DsUpdateInfo> Updates = new()
    {
        new()
        {
            Commander = Commander.Protoss,
            Time = new DateTime(2022, 03, 28),
            Changes = 
@" - Dark Templar will now use Shadow Stride to close gaps, rather than retreat.
 - Dark Templar now have the ""Shadow Retreat"" ability, which will use Shadow Stride to retreat when on low hp if set to autocast. Disabled by default."
        },
        new()
        {
            Commander = Commander.Terran,
            Time = new DateTime(2022, 03, 28),
            Changes =
@" - Ghost EMP AI improved to be less likely to overlap blasts."
        },
        new()
        {
            Commander = Commander.Vorazun,
            Time = new DateTime(2022, 03, 28),
            Changes =
@" - Dark Templar will now use Shadow Stride to close gaps, rather than retreat.
 - Dark Templar now have the ""Shadow Retreat"" ability, which will use Shadow Stride to retreat when on low hp if set to autocast. Disabled by default."
        }


    };
}

public record DsUpdateInfo
{
    public Commander Commander { get; set; }
    public DateTime Time { get; set; }
    public string Changes { get; set; } = string.Empty;
}
