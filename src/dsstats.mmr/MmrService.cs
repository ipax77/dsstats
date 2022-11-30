
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
                                                                    int mmrChangesAppendId,
                                                                    bool dry = false)
    {
        List<MmrChange> mmrChanges = new();
        for (int i = 0; i < replays.Count; i++)
        {
            RatingType ratingType = GetRatingType(replays[i]);
            if (ratingType == RatingType.None)
            {
                continue;
            }

            try
            {
                var changes = ProcessReplay(replays[i], mmrIdRatings[ratingType], cmdrMmrDic, mmrOptions);
                if (changes != null)
                {
                    mmrChanges.Add(changes);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (!dry && mmrChanges.Count > 100000)
            {
                mmrChangesAppendId = await ratingRepository.UpdateMmrChanges(mmrChanges, mmrChangesAppendId);
                mmrChanges.Clear();
                mmrChanges = new List<MmrChange>();
            }
        }

        if (!dry && mmrChanges.Any())
        {
            mmrChangesAppendId = await ratingRepository.UpdateMmrChanges(mmrChanges, mmrChangesAppendId);
        }
        return mmrIdRatings;
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

        return new MmrChange() { Hash = replay.ReplayHash, ReplayId = replay.ReplayId, Changes = mmrChanges };
    }

    private static int GetMmrId(PlayerDsRDto player)
    {
        return player.PlayerId; //ToDo
    }

    public static RatingType GetRatingType(ReplayDsRDto replayDsRDto)
    {
        if (replayDsRDto.GameMode == GameMode.Commanders || replayDsRDto.GameMode == GameMode.CommandersHeroic)
        {
            return RatingType.Cmdr;
        }
        else if (replayDsRDto.GameMode == GameMode.Standard)
        {
            return RatingType.Std;
        }
        else
        {
            return RatingType.None;
        }
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