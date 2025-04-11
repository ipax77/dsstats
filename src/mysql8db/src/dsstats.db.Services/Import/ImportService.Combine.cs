using dsstats.db.Services.Ratings;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db.Services.Import;

public partial class ImportService
{
    private readonly double dateOverflow = 0.5;
    private async Task InitArcadeRep(List<ReplayCalcDto> dsstatsReplays, ArcadeCombineData data, DsstatsContext context)
    {
        var oldestReplayDate = dsstatsReplays.First().GameTime.AddDays(-dateOverflow);
        var latestReplayDate = dsstatsReplays.Last().GameTime.AddDays(dateOverflow);

        data.ChunkInfos.Clear();

        var chunkInfo = new ArcadeChunkInfo()
        {
            StartTime = oldestReplayDate.AddDays(-1),
            EndTime = latestReplayDate.AddDays(1),
        };
        data.ChunkInfos.Add(chunkInfo);
        data.CurrentChunkInfoIndex = 0;
        await LoadCurrentChunkInfoArcadeReplays(data, context);
    }

    private async Task LoadCurrentChunkInfoArcadeReplays(ArcadeCombineData data, DsstatsContext context)
    {
        int preserveCount = 10_000;
        List<ReplayCalcDto> preserveCalcDtos = [];
        if (data.CurrentArcadeCalcDtos.Count > 0)
        {
            if (data.CurrentArcadeCalcDtos.Count <= preserveCount)
            {
                preserveCalcDtos = new(data.CurrentArcadeCalcDtos);
            }
            else
            {
                var skip = data.CurrentArcadeCalcDtos.Count - preserveCount;
                preserveCalcDtos = data.CurrentArcadeCalcDtos.Skip(skip).ToList();
            }
        }
        data.CurrentArcadeCalcDtos = await GetComboArcadeCalcDtos(data.ChunkInfos[data.CurrentChunkInfoIndex], context);
        data.CurrentArcadeCalcDtos = data.CurrentArcadeCalcDtos
            .Where(x => !data.MatchesInfo.ArcadeDict.ContainsKey(x.ReplayId))
            .ToList();

        if (preserveCalcDtos.Count > 0)
        {
            data.CurrentArcadeCalcDtos = preserveCalcDtos.Concat(data.CurrentArcadeCalcDtos).ToList();
        }
    }

    private async Task<List<ReplayCalcDto>> GetComboArcadeCalcDtos(ArcadeChunkInfo chunkInfo, DsstatsContext context)
    {
        var query = from r in context.ArcadeReplays
                    orderby r.ArcadeReplayId
                    where r.CreatedAt >= chunkInfo.StartTime
                        && r.CreatedAt <= chunkInfo.EndTime
                    select new ReplayCalcDto()
                    {
                        ReplayId = r.ArcadeReplayId,
                        GameTime = r.CreatedAt,
                        Duration = r.Duration,
                        GameMode = r.GameMode,
                        WinnerTeam = r.WinnerTeam,
                        IsArcade = true,
                        ReplayPlayers = context.ArcadeReplayPlayers
                                .Where(x => x.ArcadeReplayId == r.ArcadeReplayId)
                                .Select(t => new ReplayPlayerCalcDto()
                                {
                                    ReplayPlayerId = t.PlayerId,
                                    GamePos = t.SlotNumber,
                                    PlayerResult = t.PlayerResult,
                                    Team = t.Team,
                                    PlayerId = t.PlayerId
                                }).ToList()
                    };

        return await query
            .AsSplitQuery()
            .ToListAsync();
    }
}

internal record ArcadeCombineData
{
    public ReplayMatchInfo MatchesInfo { get; set; } = new();
    public List<ReplayCalcDto> CurrentArcadeCalcDtos { get; set; } = [];
    public List<ArcadeChunkInfo> ChunkInfos { get; set; } = [];
    public int CurrentChunkInfoIndex { get; set; }
}

internal record ArcadeChunkInfo
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}