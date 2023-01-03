
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace dsstats.mmr;

public static partial class MmrService
{
    public static async Task<(Dictionary<RatingType, Dictionary<int, CalcRating>>, int, List<ReplayData>)> GeneratePlayerRatings(List<ReplayDsRDto> replays,
                                                                    Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                                                    Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings,
                                                                    MmrOptions mmrOptions,
                                                                    int mmrChangesAppendId,
                                                                    IRatingRepository ratingRepository,
                                                                    bool dry = false)
    {
        List<ReplayData> replayDatas = new();
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
                var (changes, replayData) = ProcessReplay(replays[i], mmrIdRatings[ratingType], cmdrMmrDic, mmrOptions);
                
                if (changes != null)
                {
                    mmrChanges.Add(changes);
                }
                //if (replayData != null) // DEBUG
                //{
                //    replayDatas.Add(replayData);
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (!dry && mmrChanges.Count > 100000)
            {
                mmrChangesAppendId = await ratingRepository!.UpdateMmrChanges(mmrChanges, mmrChangesAppendId);
                mmrChanges.Clear();
                mmrChanges = new List<MmrChange>();
            }
        }


        if (!dry && mmrChanges.Any())
        {
            mmrChangesAppendId = await ratingRepository.UpdateMmrChanges(mmrChanges, mmrChangesAppendId);
        }
        return (mmrIdRatings, mmrChangesAppendId, replayDatas);
    }

    public static (MmrChange?, ReplayData?) ProcessReplay(ReplayDsRDto replay,
                                            Dictionary<int, CalcRating> mmrIdRatings,
                                            Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                            MmrOptions mmrOptions)
    {
        if (replay.WinnerTeam == 0)
        {
            return (null, null);
        }

        ReplayData replayData = new(replay);
        if (replayData.WinnerTeamData.Players.Length != 3 || replayData.LoserTeamData.Players.Length != 3)
        {
            return (null, null);
        }

        SetReplayData(mmrIdRatings, replayData, cmdrMmrDic, mmrOptions);

        var mmrChanges = ProcessReplay(replayData, mmrIdRatings, cmdrMmrDic, mmrOptions);
        return (new MmrChange() { Hash = replay.ReplayHash, ReplayId = replay.ReplayId, Changes = mmrChanges }, replayData);
    }

    public static List<PlChange> ProcessReplay(ReplayData replayData,
                                               Dictionary<int, CalcRating> mmrIdRatings,
                                               Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                               MmrOptions mmrOptions)
    {
        CalculateRatingsDeltas(mmrIdRatings, replayData, replayData.WinnerTeamData, mmrOptions);
        CalculateRatingsDeltas(mmrIdRatings, replayData, replayData.LoserTeamData, mmrOptions);

        if (mmrOptions.UseEquality)
        {
            FixMmrEquality(replayData.WinnerTeamData, replayData.LoserTeamData);
        }

        var mmrChanges1 = AddPlayersRankings(mmrIdRatings, replayData.WinnerTeamData, replayData.GameTime, replayData.Maxkillsum);
        var mmrChanges2 = AddPlayersRankings(mmrIdRatings, replayData.LoserTeamData, replayData.GameTime, replayData.Maxkillsum);
        var mmrChanges = mmrChanges1.Concat(mmrChanges2).ToList();

        if (mmrOptions.UseCommanderMmr && replayData.Maxleaver < 90)
        {
            SetCommandersComboMmr(replayData.WinnerTeamData, cmdrMmrDic);
            SetCommandersComboMmr(replayData.LoserTeamData, cmdrMmrDic);
        }

        return mmrChanges;
    }

    public static int GetMmrId(PlayerDsRDto player)
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