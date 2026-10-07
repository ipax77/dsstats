using dsstats.shared;
using dsstats.shared.Arcade;

namespace sc2arcade.crawler;

/// <summary>Shared eligibility and conversion rules for live import and archive inspection.</summary>
public static class ArcadeReplayConverter
{
    public static ArcadeConversion Convert(LobbyResult result)
    {
        if (result.SlotsHumansTaken != 6) return new(null, "HumanCountNotSix");
        if (result.Match == null || result.Match.ProfileMatches.Count == 0)
            return new(null, "MissingMatchResults");

        GameMode mode = result.MapVariantMode switch
        {
            "3V3" or "Standard" => GameMode.Standard,
            "3V3 Commanders" or "Commanders" => GameMode.Commanders,
            "Heroic Commanders" => GameMode.CommandersHeroic,
            _ => GameMode.None
        };
        if (mode == GameMode.None) return new(null, "UnsupportedGameMode");
        var winners = result.Match.ProfileMatches.Where(p => p.Decision == "win").ToList();
        if (winners.Count == 0) return new(null, "NoWinners");
        int team1 = 0, team2 = 0;
        foreach (var winner in winners)
        {
            var slot = result.Slots.FirstOrDefault(s => s.Profile?.ProfileId == winner.Profile.ProfileId
                && s.Profile.RealmId == winner.Profile.RealmId && s.Profile.RegionId == winner.Profile.RegionId);
            if (slot?.Team == 1) team1++;
            if (slot?.Team == 2) team2++;
        }
        bool unknownWinner = team1 == team2;
        if (result.Match.ProfileMatches.Any(p => p.Profile.ProfileId == 0))
            return new(null, "MatchProfileIdZero", unknownWinner);
        if (result.Slots.Any(s => s.Profile?.ProfileId == 0))
            return new(null, "SlotProfileIdZero", unknownWinner);

        return new(new ArcadeReplayDto
        {
            RegionId = result.RegionId,
            BnetRecordId = result.BnetRecordId,
            BnetBucketId = result.BnetBucketId,
            GameMode = mode,
            CreatedAt = result.CreatedAt,
            Duration = result.Match.CompletedAt == null || result.ClosedAt == null ? 0
                : System.Convert.ToInt32((result.Match.CompletedAt.Value - result.ClosedAt.Value).TotalSeconds),
            PlayerCount = 6,
            WinnerTeam = unknownWinner ? 0 : team1 > team2 ? 1 : 2,
            Players = result.Slots.Select(s => new ArcadeReplayPlayerDto
            {
                SlotNumber = s.SlotNumber ?? 0,
                Team = s.Team ?? 0,
                Player = new()
                {
                    Name = s.Name,
                    ToonId = new()
                    {
                        Realm = s.Profile?.RealmId ?? 0,
                        Region = s.Profile?.RegionId ?? 0,
                        Id = s.Profile?.ProfileId ?? 0
                    }
                }
            }).ToList()
        }, null, unknownWinner);
    }
}

public record ArcadeConversion(ArcadeReplayDto? Replay, string? RejectionReason, bool UnknownWinner = false);
