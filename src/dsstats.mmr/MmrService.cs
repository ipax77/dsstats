
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;

namespace dsstats.mmr;

public static partial class MmrService
{

    public record CalcRatingResult
    {
        public Dictionary<RatingType, Dictionary<int, CalcRating>> CalcRatings { get; set; } = new();
        public int ReplayRatingAppendId { get; set; }
        public int ReplayPlayerRatingAppendId { get; set; }
        public List<ReplayData> ReplayData { get; set; } = new();
    }

    public record CalcRatingRequest
    {
        public List<ReplayDsRDto> ReplayDsRDtos { get; set; } = new();
        public Dictionary<CmdrMmmrKey, CmdrMmmrValue> CmdrMmrDic { get; set; } = new();
        public Dictionary<RatingType, Dictionary<int, CalcRating>> MmrIdRatings { get; set; } = new();
        public MmrOptions MmrOptions { get; set; } = new(true);
        public int ReplayRatingAppendId { get; set; }
        public int ReplayPlayerRatingAppendId { get; set; }
    }

    public static async Task<CalcRatingResult> GeneratePlayerRatings(CalcRatingRequest request,
                                                                    IRatingRepository ratingRepository,
                                                                    bool dry = false)
    {
        List<ReplayData> replayDatas = new();
        List<ReplayRatingDto> replayRatingDtos = new();

        CalcRatingResult result = new()
        {
            ReplayRatingAppendId = request.ReplayRatingAppendId,
            ReplayPlayerRatingAppendId = request.ReplayPlayerRatingAppendId
        };

        for (int i = 0; i < request.ReplayDsRDtos.Count; i++)
        {
            var replay = request.ReplayDsRDtos[i];
            RatingType ratingType = GetRatingType(replay);
            if (ratingType == RatingType.None)
            {
                continue;
            }

            try
            {
                var (replayRatingDto, replayData) = ProcessReplay(replay, request.MmrIdRatings[ratingType], request.CmdrMmrDic, request.MmrOptions);

                if (replayRatingDto != null)
                {
                    replayRatingDto.RatingType = ratingType;
                    replayRatingDtos.Add(replayRatingDto);
                }
                if (dry)
                {
                    if (replayData != null)
                    {
                        replayDatas.Add(replayData);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (!dry && replayRatingDtos.Count > 100000)
            {
                (result.ReplayRatingAppendId, result.ReplayPlayerRatingAppendId)
                    = await ratingRepository.UpdateMmrChanges(replayRatingDtos, result.ReplayRatingAppendId, result.ReplayPlayerRatingAppendId);
                replayRatingDtos.Clear();
                replayRatingDtos = new List<ReplayRatingDto>();
            }
        }


        if (!dry && replayRatingDtos.Any())
        {
            (result.ReplayRatingAppendId, result.ReplayPlayerRatingAppendId)
                = await ratingRepository.UpdateMmrChanges(replayRatingDtos, result.ReplayRatingAppendId, result.ReplayPlayerRatingAppendId);
        }

        result.ReplayData = replayDatas;
        result.CalcRatings = request.MmrIdRatings;

        return result;
    }

    public static (ReplayRatingDto?, ReplayData?) ProcessReplay(ReplayDsRDto replay,
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

        var replayRatingDto = ProcessReplay(replayData, mmrIdRatings, cmdrMmrDic, mmrOptions);

        return (replayRatingDto, replayData);
    }

    public static ReplayRatingDto ProcessReplay(ReplayData replayData,
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

        if (mmrOptions.UseCommanderMmr && !replayData.IsStd && replayData.Maxleaver < 90)
        {
            SetCommandersComboMmr(replayData, replayData.WinnerTeamData, cmdrMmrDic);
            SetCommandersComboMmr(replayData, replayData.LoserTeamData, cmdrMmrDic);
        }

        return new()
        {
            LeaverType = replayData.LeaverType,
            ReplayId = replayData.ReplayId,
            RepPlayerRatings = mmrChanges1.Concat(mmrChanges2).ToList()
        };
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

    public static LeaverType GetLeaverType(ReplayDsRDto replay)
    {
        if (replay.Maxleaver < 90)
        {
            return LeaverType.None;
        }

        var leaverCount = replay.ReplayPlayers
            .Where(x => x.Duration < replay.Duration - 90)
            .Count();

        if (leaverCount == 0)
        {
            return LeaverType.None; // Corrupt or mocked Replay
        }

        if (leaverCount == 1)
        {
            return LeaverType.OneLeaver;
        }

        if (leaverCount > 2)
        {
            return LeaverType.MoreThanTwo;
        }

        var leaverPlayers = replay.ReplayPlayers.Where(x => x.Duration < replay.Duration - 90);
        var teamsCount = leaverPlayers.Select(s => s.Team).Distinct().Count();

        if (teamsCount == 1)
        {
            return LeaverType.TwoSameTeam;
        }
        else
        {
            return LeaverType.OneEachTeam;
        }
    }

    public static double GetLeaverImpactForOneEachTeam(ReplayDsRDto replay)
    {
        var leaverPlayers = replay.ReplayPlayers.Where(x => x.Duration < replay.Duration - 90).ToList();

        if (leaverPlayers.Count != 2)
        {
            throw new ArgumentOutOfRangeException(nameof(replay));
        }

        if (Math.Abs(leaverPlayers[0].Duration - leaverPlayers[1].Duration) > replay.Duration * 0.15)
        {
            return 1;
        }

        return 0.5;
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

