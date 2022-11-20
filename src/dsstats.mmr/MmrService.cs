
using pax.dsstats.shared;

namespace dsstats.mmr;

public static partial class MmrService
{
    private const double eloK = 64; // default 32
    private const double eloK_mult = 12.5;
    private const double clip = eloK * eloK_mult; //shouldn't be bigger than startMmr!
    public const float startMmr = 1000.0f;
    private const double consistencyImpact = 0.50;
    private const double consistencyDeltaMult = 0.15;
    private const double AntiSynergyPercentage = 0.50;
    private const double SynergyPercentage = 1 - AntiSynergyPercentage;
    private const double OwnMatchupPercentage = 1.0 / 3;
    private const double MatesMatchupsPercentage = (1 - OwnMatchupPercentage) / 2;

    public static async Task<(Dictionary<int, CalcRating>, float maxMmr)> GeneratePlayerRatings(List<ReplayDsRDto> replays,
                                                                    Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                                                    Dictionary<int, CalcRating> mmrIdRatings,
                                                                    float maxMmr,
                                                                    IRatingRepository ratingRepository,
                                                                    MmrOptions mmrOptions,
                                                                    bool dry = false)
    {
        List<ReplayPlayerMmrChange> mmrChanges = new();
        for (int i = 0; i < replays.Count; i++)
        {
            (maxMmr, var changes) = ProcessReplay(replays[i], mmrIdRatings, cmdrMmrDic, mmrOptions, maxMmr);
            mmrChanges.AddRange(changes);

            if (!dry && mmrChanges.Count > 100000)
            {
                await ratingRepository.UpdateReplayPlayerMmrChanges(mmrChanges);
                mmrChanges.Clear();
                mmrChanges = new List<ReplayPlayerMmrChange>();
            }
        }

        if (!dry && mmrChanges.Any())
        {
            await ratingRepository.UpdateReplayPlayerMmrChanges(mmrChanges);
        }
        return (mmrIdRatings, maxMmr);
    }

    public static List<PlayerInfo> GeneratePlayerInfos(List<ReplayDsRDto> replays, Dictionary<int, CalcRating> mmrIdRatings, RatingType ratingType)
    {
        List<PlayerInfo> ratings = new();

        foreach (var player in replays.SelectMany(s => s.ReplayPlayers).Select(s => s.Player).Distinct())
        {
            var mmrId = GetMmrId(player);
            if (mmrIdRatings.ContainsKey(mmrId))
            {
                var rating = mmrIdRatings[mmrId];
                ratings.Add(new()
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    ToonId = player.ToonId,
                    RegionId = player.RegionId,
                    Ratings = new()
                    {
                        new() 
                        {
                            PlayerId = player.PlayerId,
                            Type = ratingType,
                            Games = rating.Games,
                            Wins = rating.Wins,
                            Mvp = rating.Mvp,
                            Mmr = rating.Mmr,
                            MmrOverTime = rating.MmrOverTime,
                            Consistency = rating.Consistency,
                            Uncertainty = rating.Uncertainty
                        }
                    }
                });
            }
        }
        return ratings;
    }

    private static (float, List<ReplayPlayerMmrChange>) ProcessReplay(ReplayDsRDto replay,
                                      Dictionary<int, CalcRating> mmrIdRatings,
                                      Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                      MmrOptions mmrOptions,
                                      float maxMmr)
    {
        if (replay.WinnerTeam == 0)
        {
            return (maxMmr, new());
        }

        ReplayProcessData replayProcessData = new(replay);
        if (replayProcessData.WinnerTeamData.Players.Length != 3 || replayProcessData.LoserTeamData.Players.Length != 3)
        {
            return (maxMmr, new());
        }

        SetMmrs(mmrIdRatings, cmdrMmrDic, replayProcessData.WinnerTeamData, replay.GameTime);
        SetMmrs(mmrIdRatings, cmdrMmrDic, replayProcessData.LoserTeamData, replay.GameTime);

        SetExpectationsToWin(mmrIdRatings, replayProcessData);

        var max1 = CalculateRatingsDeltas(mmrIdRatings, replayProcessData, replayProcessData.WinnerTeamData, mmrOptions, maxMmr);
        var max2 = CalculateRatingsDeltas(mmrIdRatings, replayProcessData, replayProcessData.LoserTeamData, mmrOptions, maxMmr);

        // Adjust Loser delta
        foreach (var loserPlayer in replayProcessData.LoserTeamData.Players)
        {
            loserPlayer.PlayerMmrDelta *= -1;
            loserPlayer.PlayerConsistencyDelta *= -1;
            loserPlayer.CommanderMmrDelta *= -1;
        }
        // Adjust Leaver delta
        foreach (var winnerPlayer in replayProcessData.WinnerTeamData.Players)
        {
            if (winnerPlayer.IsLeaver)
            {
                winnerPlayer.PlayerMmrDelta *= -1;
                winnerPlayer.PlayerConsistencyDelta *= -1;
                winnerPlayer.CommanderMmrDelta = 0;
            }
        }

        FixMmrEquality(replayProcessData.WinnerTeamData, replayProcessData.LoserTeamData);


        var mmrChanges1 = AddPlayersRankings(mmrIdRatings, replayProcessData.WinnerTeamData, replay.GameTime, replay.Maxkillsum);
        var mmrChanges2 = AddPlayersRankings(mmrIdRatings, replayProcessData.LoserTeamData, replay.GameTime, replay.Maxkillsum);
        mmrChanges1.AddRange(mmrChanges2);

        SetCommandersComboMmr(replayProcessData.WinnerTeamData, cmdrMmrDic);
        SetCommandersComboMmr(replayProcessData.LoserTeamData, cmdrMmrDic);

        return (Math.Max(max1, max2), mmrChanges1);
    }

    private static int GetMmrId(PlayerDsRDto player)
    {
        return player.PlayerId;
    }

}

public struct CmdrMmmrKey
{
    public CmdrMmmrKey(Commander race, Commander oppRace)
    {
        Race = race;
        OppRace = oppRace;
    }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
}

public record CmdrMmmrValue
{
    public double SynergyMmr { get; set; }
    public double AntiSynergyMmr { get; set; }
}