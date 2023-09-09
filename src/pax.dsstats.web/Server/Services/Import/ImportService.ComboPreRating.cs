using AutoMapper.QueryableExtensions;
using dsstats.mmr;
using dsstats.ratings.lib;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Services.Import;

public partial class ImportService
{
    public async Task SetComboPreRatings(Replay replay)
    {
        if ((DateTime.UtcNow - replay.GameTime) > TimeSpan.FromHours(1))
        {
           return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        var waitResult = await ratingsService.ratingSs.WaitAsync(60000);
        if (waitResult == false)
        {
            return;
        }

        try
        {
            var calcDto = await context.Replays
                .Where(x => x.ReplayId == replay.ReplayId)
                .Select(s => new shared.Calc.CalcDto()
                {
                    DsstatsReplayId = replay.ReplayId,
                    GameTime = s.GameTime,
                    GameMode = (int)s.GameMode,
                    Duration = s.Duration,
                    TournamentEdition = s.TournamentEdition,

                })
                .FirstOrDefaultAsync();

            if (calcDto == null)
            {
                return;
            }

            var players = await context.ReplayPlayers
                .Where(x => x.ReplayId == replay.ReplayId)
                .Select(s => new shared.Calc.PlayerCalcDto()
                {
                    ReplayPlayerId = s.ReplayPlayerId,
                    GamePos = s.GamePos,
                    PlayerResult = (int)s.PlayerResult,
                    IsLeaver = s.Duration < calcDto.Duration - 90,
                    ProfileId = s.Player.ToonId,
                    RegionId = s.Player.RegionId,
                    RealmId = s.Player.RealmId
                })
                .ToListAsync();

            calcDto = calcDto with { Players = players };

            var ratingType = CalcService.GetRatingType(calcDto);

            if (ratingType == 0)
            {
                return;
            }

            var calcRatings = await GetComboCalcRatings(replay, ratingType, context);

            CalcRatingRequest calcRequest = new()
            {
                MmrIdRatings = new()
                {
                    {ratingType, calcRatings}
                }
            };

            var replayRating = CalcService.ProcessReplay(calcDto, calcRequest);

            if (replayRating == null)
            {
                return;
            }

            await SaveComboPreRating(replay, replayRating, context);
        }
        catch (Exception ex)
        {
            logger.LogError($"failed generating combo preRating: {ex.Message}");
        }
        finally
        {
            ratingsService.ratingSs.Release();
        }
    }

    private async Task SaveComboPreRating(Replay replay, shared.Calc.ReplayRatingDto replayRatingDto, ReplayContext context)
    {
        ComboReplayRating comboReplayRating = new()
        {
            RatingType = (RatingType)replayRatingDto.RatingType,
            LeaverType = (LeaverType)replayRatingDto.LeaverType,
            ExpectationToWin = Math.Round(replayRatingDto.ExpectationToWin, 2),
            ReplayId = replay.ReplayId,
            IsPreRating = true
        };
        context.ComboReplayRatings.Add(comboReplayRating);

        foreach (var player in replayRatingDto.RepPlayerRatings)
        {
            ComboReplayPlayerRating playerRating = new()
            {
                GamePos = player.GamePos,
                Rating = Convert.ToInt32(player.Rating),
                Change = Math.Round(player.RatingChange, 2),
                Games = player.Games,
                Consistency = Math.Round(player.Consistency, 2),
                Confidence = Math.Round(player.Confidence, 2),
                ReplayPlayerId = player.ReplayPlayerId
            };
            context.ComboReplayPlayerRatings.Add(playerRating);
        }
        await context.SaveChangesAsync();
    }

    private async Task<Dictionary<dsstats.shared.Calc.PlayerId, dsstats.shared.Calc.CalcRating>> GetComboCalcRatings(Replay replay, int ratingType, ReplayContext context)
    {
        Dictionary<shared.Calc.PlayerId, dsstats.shared.Calc.CalcRating> calcRatings = new();


        var playerIds = replay.ReplayPlayers.Select(s => s.PlayerId).ToList();

        var calcDtos = await context.ComboPlayerRatings
            .Where(x => x.RatingType == (RatingType)ratingType
                && playerIds.Contains(x.Player.PlayerId))
            .Select(s => new PlayerRatingReplayCalcDto()
            {
                Rating = s.Rating,
                Games = s.Games,
                Consistency = s.Consistency,
                Confidence = s.Confidence,
                Player = new PlayerReplayCalcDto()
                {
                    PlayerId = s.PlayerId,
                    ToonId = s.Player.ToonId,
                    RegionId = s.Player.RegionId,
                    RealmId = s.Player.RealmId
                }
            })
            .ToListAsync();

        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            var calcDto = calcDtos.FirstOrDefault(f => f.Player.PlayerId == replayPlayer.PlayerId);

            if (calcDto == null)
            {
                continue;
            }

            shared.Calc.PlayerId playerId = new() { ProfileId = calcDto.Player.ToonId, RegionId = calcDto.Player.RegionId, RealmId = calcDto.Player.RealmId };

            dsstats.shared.Calc.CalcRating calcRating = new()
            {
                PlayerId = playerId,
                Games = calcDto?.Games ?? 0,
                Mmr = calcDto?.Rating ?? 1000.0,
                Consistency = calcDto?.Consistency ?? 0,
                Confidence = calcDto?.Confidence ?? 0,
            };

            calcRatings[playerId] = calcRating;
        }

        return calcRatings;
    }
}
