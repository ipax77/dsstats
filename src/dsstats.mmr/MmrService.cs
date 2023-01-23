
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
    }

    public record CalcRatingRequest
    {
        public List<ReplayDsRDto> ReplayDsRDtos { get; set; } = new();
        public Dictionary<CmdrMmrKey, CmdrMmrValue> CmdrMmrDic { get; set; } = new();
        public Dictionary<RatingType, Dictionary<int, CalcRating>> MmrIdRatings { get; set; } = new();
        public MmrOptions MmrOptions { get; set; } = new(true);
        public int ReplayRatingAppendId { get; set; }
        public int ReplayPlayerRatingAppendId { get; set; }
    }

    public static async Task<CalcRatingResult> GeneratePlayerRatings(CalcRatingRequest request,
                                                                    IRatingRepository ratingRepository,
                                                                    bool dry = false)
    {
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

            var replayRatingDto = ProcessReplay(replay, request.MmrIdRatings[ratingType], request.CmdrMmrDic, request.MmrOptions);

            if (replayRatingDto != null)
            {
                replayRatingDtos.Add(replayRatingDto);
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

        result.CalcRatings = request.MmrIdRatings;

        return result;
    }

    public static ReplayRatingDto? ProcessReplay(ReplayDsRDto replay,
                                            Dictionary<int, CalcRating> mmrIdRatings,
                                            Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrMmrDic,
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

        CalculateRatingsDeltas(replayData, replayData.WinnerTeamData, mmrOptions);
        CalculateRatingsDeltas(replayData, replayData.LoserTeamData, mmrOptions);

        if (mmrOptions.UseEquality)
        {
            FixMmrEquality(replayData.WinnerTeamData, replayData.LoserTeamData);
        }

        var mmrChanges1 = AddPlayersRankings(mmrIdRatings, replayData.WinnerTeamData, replayData.ReplayDsRDto.GameTime, replayData.ReplayDsRDto.Maxkillsum);
        var mmrChanges2 = AddPlayersRankings(mmrIdRatings, replayData.LoserTeamData, replayData.ReplayDsRDto.GameTime, replayData.ReplayDsRDto.Maxkillsum);

        if (mmrOptions.UseCommanderMmr && replayData.ReplayDsRDto.Maxleaver < 90)
        {
            SetCommandersComboMmr(replayData, replayData.WinnerTeamData, cmdrMmrDic);
            SetCommandersComboMmr(replayData, replayData.LoserTeamData, cmdrMmrDic);
        }

        var replayRatingDto = new ReplayRatingDto()
        {
            RatingType = replayData.RatingType,
            LeaverType = replayData.LeaverType,
            ReplayId = replayData.ReplayDsRDto.ReplayId,
            RepPlayerRatings = mmrChanges1.Concat(mmrChanges2).ToList()
        };

        return replayRatingDto;
    }

    public static int GetMmrId(PlayerDsRDto player)
    {
        return player.PlayerId; //ToDo
    }

    public static RatingType GetRatingType(ReplayDsRDto replayDsRDto)
    {
        if (replayDsRDto.TournamentEdition && replayDsRDto.GameMode == GameMode.Commanders)
        {
            return RatingType.CmdrTE;
        }
        else if (replayDsRDto.TournamentEdition && replayDsRDto.GameMode == GameMode.Standard)
        {
            return RatingType.StdTE;
        }
        else if (replayDsRDto.GameMode == GameMode.Commanders || replayDsRDto.GameMode == GameMode.CommandersHeroic)
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

public struct CmdrMmrKey
{
    public CmdrMmrKey(Commander race, Commander oppRace)
    {
        Race = race;
        OppRace = oppRace;
    }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
}

public record CmdrMmrValue
{
    public double SynergyMmr { get; set; }
    public double AntiSynergyMmr { get; set; }
}

