namespace dsstats.api.InHouse;

public sealed class InHouseAuthOptions
{
    public const string SectionName = "InHouseAuth";

    public string ProductionOrigin { get; set; } = "https://mydsstats.pax77.org";
    public string ProductionRpId { get; set; } = "mydsstats.pax77.org";
    public string LocalRpId { get; set; } = "localhost";
    public string[] LocalOrigins { get; set; } = ["https://localhost:7039", "http://localhost:5190"];
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(20);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan DeviceLinkLifetime { get; set; } = TimeSpan.FromMinutes(10);
}
