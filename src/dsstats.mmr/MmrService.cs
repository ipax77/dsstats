
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using dsstats.mmr.Extensions;
using dsstats.mmr.ProcessData;
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

    private const double confidenceImpact = 0.90;
    private const double distributionMult = 1.0 / (1/*2*/);

    private const double antiSynergyPercentage = 0.50;
    private const double synergyPercentage = 1 - antiSynergyPercentage;
    private const double ownMatchupPercentage = 1.0 / 3;
    private const double matesMatchupsPercentage = (1 - ownMatchupPercentage) / 2;

    public static async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GeneratePlayerRatings(List<ReplayDsRDto> replays,
                                                                    Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                                                    Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings,
                                                                    IRatingRepository ratingRepository,
                                                                    MmrOptions mmrOptions,
                                                                    bool dry = false)
    {
        List<MmrChange> mmrChanges = new();
        for (int i = 0; i < replays.Count; i++)
        {
            RatingType ratingType = GetRatingType(replays[i]);
            if (ratingType == RatingType.None) {
                continue;
            }

            var changes = ProcessReplay(replays[i], mmrIdRatings[ratingType], cmdrMmrDic, mmrOptions);

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
        return mmrIdRatings;
    }

    public static RatingType GetRatingType(ReplayDsRDto replayDsRDto)
    {
        if (replayDsRDto.GameMode == GameMode.Commanders || replayDsRDto.GameMode == GameMode.CommandersHeroic) {
            return RatingType.Cmdr;
        } else if (replayDsRDto.GameMode == GameMode.Standard) {
            return RatingType.Std;
        } else {
            return RatingType.None;
        }
    }

    private static MmrChange? ProcessReplay(ReplayDsRDto replay,
                                      Dictionary<int, CalcRating> mmrIdRatings,
                                      Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                      MmrOptions mmrOptions)
    {
        if (replay.WinnerTeam == 0)
        {
            return null;
        }

        ReplayData replayData = new(replay);
        if (replayData.WinnerTeamData.Players.Length != 3 || replayData.LoserTeamData.Players.Length != 3)
        {
            return null;
        }

        SetReplayData(mmrIdRatings, replayData, cmdrMmrDic, mmrOptions);

        CalculateRatingsDeltas(mmrIdRatings, replayData, replayData.WinnerTeamData, mmrOptions);
        CalculateRatingsDeltas(mmrIdRatings, replayData, replayData.LoserTeamData, mmrOptions);


        FixMmrEquality(replayData.WinnerTeamData, replayData.LoserTeamData);


        var mmrChanges1 = AddPlayersRankings(mmrIdRatings, replayData.WinnerTeamData, replay.GameTime, replay.Maxkillsum);
        var mmrChanges2 = AddPlayersRankings(mmrIdRatings, replayData.LoserTeamData, replay.GameTime, replay.Maxkillsum);
        var mmrChanges = mmrChanges1.Concat(mmrChanges2).ToList();

        if (mmrOptions.UseCommanderMmr && replay.Maxleaver < 90)
        {
            SetCommandersComboMmr(replayData.WinnerTeamData, cmdrMmrDic);
            SetCommandersComboMmr(replayData.LoserTeamData, cmdrMmrDic);
        }

        return new MmrChange() { Hash = replay.ReplayHash, Changes = mmrChanges };
    }

    private static int GetMmrId(PlayerDsRDto player)
    {
        return player.PlayerId; //ToDo
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