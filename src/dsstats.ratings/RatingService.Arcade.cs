using dsstats.db8;
using dsstats.shared.Calc;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Microsoft.Extensions.Logging;
using dsstats.shared.Interfaces;
using System.Collections.Frozen;

namespace dsstats.ratings;

public partial class RatingService
{
    private async Task ProduceArcadeRatings(bool recalc)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingSaveService = scope.ServiceProvider.GetRequiredService<IRatingsSaveService>();

        DsstatsCalcRequest dsstatsRequest = new()
        {
            FromDate = new DateTime(2021, 2, 1),
            GameModes = new List<int>() { 3, 4, 7 },
            Skip = 0,
            Take = 100000
        };

        CalcRatingRequest ratingRequest = new()
        {
            RatingCalcType = RatingCalcType.Dsstats,
            MmrIdRatings = new()
                    {
                        { 1, new() },
                        { 2, new() },
                        { 3, new() },
                        { 4, new() }
                    },
            BannedPlayers = new Dictionary<PlayerId, bool>().ToFrozenDictionary()
        };

        await CreateMaterializedReplays();
        var arcadeCalcDtos = await GetMaterializedArcadeCalcDtos(dsstatsRequest, context);

        List<shared.Calc.ReplayRatingDto> replayRatings = new();

        while (arcadeCalcDtos.Count > 0)
        {
            for (int i = 0; i < arcadeCalcDtos.Count; i++)
            {
                var calcDto = arcadeCalcDtos[i];
                CorrectPlayerResults(calcDto);
                var rating = ratings.lib.Ratings.ProcessReplay(calcDto, ratingRequest);
                if (rating is not null)
                {
                    replayRatings.Add(rating);
                }
            }

            (ratingRequest.ReplayRatingAppendId, ratingRequest.ReplayPlayerRatingAppendId) =
                await ratingSaveService.SaveArcadeStepResult(replayRatings,
                                                   ratingRequest.ReplayRatingAppendId,
                                                   ratingRequest.ReplayPlayerRatingAppendId);
            replayRatings = new();
            dsstatsRequest.Skip += dsstatsRequest.Take;
            arcadeCalcDtos = await GetMaterializedArcadeCalcDtos(dsstatsRequest, context);
        }

        await ratingSaveService.SaveArcadePlayerRatings(ratingRequest.MmrIdRatings, ratingRequest.SoftBannedPlayers);
    }

    private async Task CreateMaterializedReplays()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();

        try
        {

            using var connection = new MySqlConnection(options.Value.ImportConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandTimeout = 120;
            command.CommandText = "CALL CreateMaterializedArcadeReplays();";

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed creating materialized arcade replays: {error}", ex.Message);
        }
    }

    private async Task<List<CalcDto>> GetArcadeCalcDtos(DsstatsCalcRequest request, ReplayContext context)
    {
        return await context.ArcadeReplays
            .Where(x => x.PlayerCount == 6
             && x.Duration >= 300
             && x.WinnerTeam > 0
             && x.CreatedAt >= request.FromDate
             && request.GameModes.Contains((int)x.GameMode)
             && x.TournamentEdition == false)
            .OrderBy(o => o.CreatedAt)
                .ThenBy(o => o.ArcadeReplayId)
            .Select(s => new CalcDto()
            {
                ReplayId = s.ArcadeReplayId,
                GameTime = s.CreatedAt,
                Duration = s.Duration,
                GameMode = (int)s.GameMode,
                TournamentEdition = s.TournamentEdition,
                Players = s.ArcadeReplayPlayers.Select(t => new PlayerCalcDto()
                {
                    ReplayPlayerId = t.ArcadeReplayPlayerId,
                    GamePos = t.SlotNumber,
                    PlayerResult = (int)t.PlayerResult,
                    Team = t.Team,
                    PlayerId = new(t.ArcadePlayer.ProfileId, t.ArcadePlayer.RealmId, t.ArcadePlayer.RegionId)
                }).ToList()
            })
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();
    }

    private async Task<List<CalcDto>> GetMaterializedArcadeCalcDtos(DsstatsCalcRequest request, ReplayContext context)
    {
        var query = from r in context.MaterializedArcadeReplays
                    where r.CreatedAt >= request.FromDate
                    orderby r.MaterializedArcadeReplayId
                    select new CalcDto()
                    {
                        ReplayId = r.ArcadeReplayId,
                        GameTime = r.CreatedAt,
                        Duration = r.Duration,
                        GameMode = (int)r.GameMode,
                        WinnerTeam = r.WinnerTeam,
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
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();
    }

    private static void CorrectPlayerResults(CalcDto calcDto)
    {
        foreach (var pl in calcDto.Players)
        {
            pl.PlayerResult = pl.Team == calcDto.WinnerTeam ? 1 : 2;
        }
    }
}
