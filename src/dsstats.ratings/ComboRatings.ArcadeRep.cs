using dsstats.shared.Calc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.ratings;

public partial class ComboRatings
{
    private readonly double dateOverflow = 0.5;
    private List<CalcDto> currentArcadeCalcDtos = [];
    private List<ArcadeChunkInfo> chunkInfos = [];
    private int currentChunkInfoIndex = 0;

    private async Task InitArcadeRep(List<CalcDto> dsstatsReplays)
    {
        // await CreateMaterializedReplays();

        var oldestReplayDate = dsstatsReplays.First().GameTime.AddDays(-dateOverflow);
        var latestReplayDate = dsstatsReplays.Last().GameTime.AddDays(dateOverflow);

        var startId = await GetStartIdAsync(oldestReplayDate);
        var endId = await GetEndIdAsync(latestReplayDate);

        chunkInfos.Clear();
        if (endId - startId > 150_000)
        {
            await CreateChunks(oldestReplayDate, latestReplayDate, startId, endId);
        }
        else
        {
            var chunkInfo = new ArcadeChunkInfo()
            {
                StartTime = oldestReplayDate,
                EndTime = latestReplayDate,
                StartId = startId,
                EndId = endId,
            };
            chunkInfos.Add(chunkInfo);
        }
        currentChunkInfoIndex = 0;
        await LoadCurrentChunkInfoArcadeReplays();
    }

    private async Task CreateChunks(DateTime oldestReplayDate, DateTime latestReplayDate, int startId, int endId)
    {
        var stepDate = oldestReplayDate;
        var stepStartId = startId;
        while (stepDate < latestReplayDate)
        {
            var stepChunkInfo = new ArcadeChunkInfo
            {
                StartTime = stepDate,
                StartId = stepStartId
            };
            stepDate = stepDate.AddDays(15);
            int stepEndId;
            if (stepDate > latestReplayDate)
            {
                stepDate = latestReplayDate;
                stepEndId = endId;
            }
            else
            {
                stepEndId = await GetEndIdAsync(stepDate);
            }
            stepChunkInfo.EndTime = stepDate;
            stepChunkInfo.EndId = stepEndId;
            chunkInfos.Add(stepChunkInfo);
            stepStartId = stepEndId + 1;
        }
        logger.LogInformation("Got {count} chunks", chunkInfos.Count);
    }

    private async Task<int> GetStartIdAsync(DateTime date)
    {
        return await context.MaterializedArcadeReplays
            .Where(x => x.CreatedAt > date)
            .OrderBy(o => o.MaterializedArcadeReplayId)
            .Select(s => s.MaterializedArcadeReplayId)
            .FirstOrDefaultAsync();
    }

    private async Task<int> GetEndIdAsync(DateTime date)
    {
        return await context.MaterializedArcadeReplays
            .Where(x => x.CreatedAt < date)
            .OrderBy(o => o.MaterializedArcadeReplayId)
            .Select(s => s.MaterializedArcadeReplayId)
            .LastOrDefaultAsync();
    }

    private async Task LoadCurrentChunkInfoArcadeReplays()
    {
        int preserveCount = 10_000;
        List<CalcDto> preserveCalcDtos = [];
        if (currentArcadeCalcDtos.Count > 0)
        {
            if (currentArcadeCalcDtos.Count <= preserveCount)
            {
                preserveCalcDtos = new(currentArcadeCalcDtos);
            }
            else
            {
                var skip = currentArcadeCalcDtos.Count - preserveCount;
                preserveCalcDtos = currentArcadeCalcDtos.Skip(skip).ToList();
            }
        }
        currentArcadeCalcDtos = await GetComboArcadeCalcDtos(chunkInfos[currentChunkInfoIndex]);
        currentArcadeCalcDtos = currentArcadeCalcDtos
            .Where(x => !matchesInfo.ArcadeDict.ContainsKey(x.ReplayId))
            .ToList();
        logger.LogInformation("Loaded chunk {index}/{count}", currentChunkInfoIndex, currentArcadeCalcDtos.Count);
        logger.LogInformation(chunkInfos[currentChunkInfoIndex].ToString());

        if (preserveCalcDtos.Count > 0)
        {
            currentArcadeCalcDtos = preserveCalcDtos.Concat(currentArcadeCalcDtos).ToList();
        }
    }

    private async Task UpdateArcadeReplays(CalcDto dsstasReplay)
    {
        var currentChunkInfo = chunkInfos[currentChunkInfoIndex];
        if (dsstasReplay.GameTime > currentChunkInfo.EndTime.AddDays(-0.5))
        {
            if (chunkInfos.Count > currentChunkInfoIndex + 1)
            {
                currentChunkInfoIndex++;
                currentChunkInfo = chunkInfos[currentChunkInfoIndex];
                await LoadCurrentChunkInfoArcadeReplays();
            }
            else
            {
                logger.LogWarning("currentChunkInfoIndex out of bounds: {current}/{count}", currentChunkInfoIndex, chunkInfos.Count);
            }
        }
    }

    // every call does get a dsstatsReplay with increase GameTime (orderby r.GameTime, r.ReplayId)
    private async Task<List<CalcDto>> GetReasonableReplays(CalcDto dsstatsReplay, HashSet<int> matchedArcadeIds)
    {
        await UpdateArcadeReplays(dsstatsReplay);
        return currentArcadeCalcDtos
            .Where(x => x.GameTime > dsstatsReplay.GameTime.AddDays(-dateOverflow)
                && x.GameTime < dsstatsReplay.GameTime.AddDays(dateOverflow)
                && x.GameMode == dsstatsReplay.GameMode
                && GetReplayRegionId(x) == GetReplayRegionId(dsstatsReplay)
                && !matchedArcadeIds.Contains(x.ReplayId)
            ).ToList();
    }

    private async Task<List<CalcDto>> GetComboArcadeCalcDtos(ArcadeChunkInfo chunkInfo)
    {
        var query = from r in context.MaterializedArcadeReplays
                    orderby r.MaterializedArcadeReplayId
                    where r.MaterializedArcadeReplayId >= chunkInfo.StartId
                        && r.MaterializedArcadeReplayId <= chunkInfo.EndId
                    select new CalcDto()
                    {
                        ReplayId = r.ArcadeReplayId,
                        GameTime = r.CreatedAt,
                        Duration = r.Duration,
                        GameMode = (int)r.GameMode,
                        WinnerTeam = r.WinnerTeam,
                        TournamentEdition = false,
                        IsArcade = true,
                        Players = context.ArcadeReplayPlayers
                                .Where(x => x.ArcadeReplayId == r.ArcadeReplayId)
                                .Select(t => new PlayerCalcDto()
                                {
                                    ReplayPlayerId = t.ArcadeReplayPlayerId,
                                    GamePos = t.SlotNumber,
                                    PlayerResult = (int)t.PlayerResult,
                                    Team = t.Team,
                                    PlayerId = new(t.ArcadePlayer.ProfileId, t.ArcadePlayer.RealmId, t.ArcadePlayer.RegionId)
                                }).ToList()
                    };

        return await query
            .AsSplitQuery()
            .ToListAsync();
    }

    internal record ArcadeChunkInfo
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int StartId { get; set; }
        public int EndId { get; set; }
    }
}
