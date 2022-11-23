
using System.Globalization;
using System.Text;
using dsstats.mmr.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace dsstats.mmr;

public static partial class MmrService
{
    private const double eloK = 64; // default 32
    private const double eloK_mult = 12.5;
    private const double clip = eloK * eloK_mult; //shouldn't be bigger than startMmr!
    public const double startMmr = 1000.0;
    private const double consistencyImpact = 0.50;
    private const double consistencyDeltaMult = 0.15;
    private const double AntiSynergyPercentage = 0.50;
    private const double SynergyPercentage = 1 - AntiSynergyPercentage;
    private const double OwnMatchupPercentage = 1.0 / 3;
    private const double MatesMatchupsPercentage = (1 - OwnMatchupPercentage) / 2;

    public static async Task<(Dictionary<int, CalcRating>, double maxMmr)> GeneratePlayerRatings(List<ReplayDsRDto> replays,
                                                                    Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                                                    Dictionary<int, CalcRating> mmrIdRatings,
                                                                    double maxMmr,
                                                                    IRatingRepository ratingRepository,
                                                                    MmrOptions mmrOptions,
                                                                    bool dry = false)
    {
        List<MmrChange> mmrChanges = new();
        for (int i = 0; i < replays.Count; i++)
        {
            (maxMmr, var changes) = ProcessReplay(replays[i], mmrIdRatings, cmdrMmrDic, mmrOptions, maxMmr);

            if (changes != null)
            {
                mmrChanges.Add(changes);
            }

            if (!dry && mmrChanges.Count > 100000)
            {
                await ratingRepository.UpdateMmrChanges(mmrChanges);
                mmrChanges.Clear();
                mmrChanges = new List<MmrChange>();
            }
        }

        if (!dry && mmrChanges.Any())
        {
            await ratingRepository.UpdateMmrChanges(mmrChanges);
        }
        return (mmrIdRatings, maxMmr);
    }

    public static Dictionary<RavenPlayer, RavenRating> GetRavenPlayers(List<PlayerDsRDto> players, Dictionary<int, CalcRating> mmrIdRatings)
    {
        Dictionary<RavenPlayer, RavenRating> ravenPlayerRatings = new();

        foreach (var player in players)
        {
            var mmrId = GetMmrId(player);

            if (mmrIdRatings.ContainsKey(mmrId))
            {
                var rating = mmrIdRatings[mmrId];
                (var main, var mainper) = rating.GetMain();
                RavenPlayer ravenPlayer = new()
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    ToonId = player.ToonId,
                    RegionId = player.RegionId,
                    IsUploader = rating.IsUploader

                };

                ravenPlayerRatings[ravenPlayer] = new()
                {
                    Games = rating.Games,
                    Wins = rating.Wins,
                    Mvp = rating.Mvp,
                    Main = main,
                    MainPercentage = mainper,
                    Mmr = rating.Mmr,
                    MmrOverTime = GetDbMmrOverTime(rating.MmrOverTime),
                    Consistency = rating.Consistency,
                    Uncertainty = rating.Uncertainty,
                };
            }
        }
        return ravenPlayerRatings;
    }

    private static (double, MmrChange?) ProcessReplay(ReplayDsRDto replay,
                                      Dictionary<int, CalcRating> mmrIdRatings,
                                      Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                      MmrOptions mmrOptions,
                                      double maxMmr)
    {
        if (replay.WinnerTeam == 0)
        {
            return (maxMmr, null);
        }

        ReplayProcessData replayProcessData = new(replay);
        if (replayProcessData.WinnerTeamData.Players.Length != 3 || replayProcessData.LoserTeamData.Players.Length != 3)
        {
            return (maxMmr, null);
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

        return (Math.Max(max1, max2), new MmrChange() { Hash = replay.ReplayHash, Changes = mmrChanges1 });
    }

    private static int GetMmrId(PlayerDsRDto player)
    {
        return player.PlayerId;
    }

    public static string? GetDbMmrOverTime(List<TimeRating> timeRatings)
    {
        if (!timeRatings.Any())
        {
            return null;
        }

        if (timeRatings.Count == 1)
        {
            return $"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{timeRatings[0].Date[2..6]}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{timeRatings[0].Date[2..6]}");

        if (timeRatings.Count > 2)
        {
            string timeStr = timeRatings[0].Date[2..6];
            for (int i = 1; i < timeRatings.Count - 1; i++)
            {
                string currentTimeStr = timeRatings[i].Date[2..6];
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(timeRatings[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{timeRatings[i].Date[2..6]}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(timeRatings.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{timeRatings.Last().Date[2..6]}");

        return sb.ToString();
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