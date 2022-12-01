using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
    private async Task<RatingsResult> GetMauiRatings(RatingsRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playerRatings = context.PlayerRatings
            .Include(i => i.Player)
            .Where(x => x.Games > 20 && x.RatingType == request.Type);


        if (!String.IsNullOrEmpty(request.Search))
        {
            var playerIds = await context.Players
                .Where(x => x.Name.ToUpper().Contains(request.Search.ToUpper()))
                .Select(s => s.PlayerId)
                .ToListAsync();

            playerRatings = playerRatings.Where(x => playerIds.Contains(x.PlayerId));
        }

        foreach (var order in request.Orders)
        {
            if (order.Property.EndsWith("Mvp"))
            {
                if (order.Ascending)
                {
                    playerRatings = playerRatings.OrderBy(o => o.Mvp * 100.0 / o.Games);
                }
                else
                {
                    playerRatings = playerRatings.OrderByDescending(o => o.Mvp * 100.0 / o.Games);
                }
            }
            else if (order.Property.EndsWith("Wins"))
            {
                if (order.Ascending)
                {
                    playerRatings = playerRatings.OrderBy(o => o.Wins * 100.0 / o.Games);
                }
                else
                {
                    playerRatings = playerRatings.OrderByDescending(o => o.Wins * 100.0 / o.Games);
                }
            }
            else if (order.Property.EndsWith("MainPercentage"))
            {
                if (order.Ascending)
                {
                    playerRatings = playerRatings.OrderBy(o => o.MainCount * 100.0 / o.Games);
                }
                else
                {
                    playerRatings = playerRatings.OrderByDescending(o => o.MainCount * 100.0 / o.Games);
                }
            }
            else if (order.Property.EndsWith("Name"))
            {
                if (order.Ascending)
                {
                    playerRatings = playerRatings.AppendOrderBy("Player.Name");
                }
                else
                {
                    playerRatings = playerRatings.AppendOrderByDescending("Player.Name");
                }
            }
            else if (order.Property.EndsWith("RegionId"))
            {
                if (order.Ascending)
                {
                    playerRatings = playerRatings.AppendOrderBy("Player.RegionId");
                }
                else
                {
                    playerRatings = playerRatings.AppendOrderByDescending("Player.RegionId");
                }
            }
            else
            {
                var property = order.Property.StartsWith("Rating.") ? order.Property[7..] : order.Property;
                if (property == "Mmr")
                {
                    property = "Rating";
                }
                if (order.Ascending)
                {
                    playerRatings = playerRatings.AppendOrderBy(property);
                }
                else
                {
                    playerRatings = playerRatings.AppendOrderByDescending(property);
                }
            }
        }

        return new RatingsResult
        {
            Count = playerRatings.Count(),
            Players = await playerRatings.Skip(request.Skip).Take(request.Take)
                .Select(s => new RavenPlayerDto()
                {
                    Name = s.Player.Name,
                    ToonId = s.Player.ToonId,
                    RegionId = s.Player.RegionId,
                    Rating = new()
                    {
                        Games = s.Games,
                        Wins = s.Wins,
                        Mvp = s.Mvp,
                        TeamGames = s.TeamGames,
                        Main = s.Main,
                        MainPercentage = Math.Round(s.MainCount * 100.0 / s.Games, 2),
                        Mmr = s.Rating
                    }
                })
                .ToListAsync(token)
        };
    }

    private async Task<UpdateResult> MauiUpdateRavenPlayers(HashSet<PlayerDsRDto> players, Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        using var connection = new SqliteConnection(Data.SqliteConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
            $@"
                INSERT OR REPLACE INTO PlayerRatings ({nameof(PlayerRating.PlayerRatingId)},{nameof(PlayerRating.RatingType)},{nameof(PlayerRating.Rating)},{nameof(PlayerRating.Games)},{nameof(PlayerRating.Wins)},{nameof(PlayerRating.Mvp)},{nameof(PlayerRating.TeamGames)},{nameof(PlayerRating.MainCount)},{nameof(PlayerRating.Main)},{nameof(PlayerRating.MmrOverTime)},{nameof(PlayerRating.Consistency)},{nameof(PlayerRating.Confidence)},{nameof(PlayerRating.IsUploader)},{nameof(PlayerRating.PlayerId)})
                VALUES ((SELECT {nameof(PlayerRating.PlayerRatingId)} from PlayerRatings where {nameof(PlayerRating.RatingType)} = $value1 AND {nameof(PlayerRating.PlayerId)} = $value13),$value1,$value2,$value3,$value4,$value5,$value6,$value7,$value8,$value9,$value10,$value11,$value12,$value13)
            ";

        List<SqliteParameter> parameters = new List<SqliteParameter>();
        for (int i = 1; i <= 13; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"$value{i}";
            command.Parameters.Add(parameter);
            parameters.Add(parameter);
        }

        foreach (var ent in mmrIdRatings)
        {
            foreach (var calcEnt in ent.Value.Values)
            {
                var main = calcEnt.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();

                parameters[0].Value = (int)ent.Key;
                parameters[1].Value = calcEnt.Mmr;
                parameters[2].Value = calcEnt.Games;
                parameters[3].Value = calcEnt.Wins;
                parameters[4].Value = calcEnt.Mvp;
                parameters[5].Value = calcEnt.TeamGames;
                parameters[6].Value = main.Value;
                parameters[7].Value = (int)main.Key;
                parameters[8].Value = GetDbMmrOverTime(calcEnt.MmrOverTime);
                parameters[9].Value = calcEnt.Consistency;
                parameters[10].Value = calcEnt.Confidence;
                parameters[11].Value = calcEnt.IsUploader;
                parameters[12].Value = calcEnt.PlayerId;
                await command.ExecuteNonQueryAsync();
            }
        }

        await transaction.CommitAsync();
        return new();
    }

    private async Task<int> MauiUpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId)
    {
        if (appendId == 0)
        {
            await DeleteReplayPlayerRatingsTable();
        }

        using var connection = new SqliteConnection(Data.SqliteConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
        $@"
                INSERT INTO ReplayPlayerRatings ({nameof(ReplayPlayerRating.ReplayPlayerRatingId)},{nameof(ReplayPlayerRating.MmrChange)},{nameof(ReplayPlayerRating.Pos)},{nameof(ReplayPlayerRating.ReplayPlayerId)},{nameof(ReplayPlayerRating.ReplayId)})
                VALUES ($value1,$value2,$value3,$value4,$value5)
            ";

        List<SqliteParameter> parameters = new List<SqliteParameter>();
        for (int i = 1; i <= 5; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"$value{i}";
            command.Parameters.Add(parameter);
            parameters.Add(parameter);
        }

        for (int i = 0; i < replayPlayerMmrChanges.Count; i++)
        {
            for (int j = 0; j < replayPlayerMmrChanges[i].Changes.Count; j++)
            {
                appendId++;
                parameters[0].Value = appendId;
                parameters[1].Value = replayPlayerMmrChanges[i].Changes[j].Change;
                parameters[2].Value = replayPlayerMmrChanges[i].Changes[j].Pos;
                parameters[3].Value = replayPlayerMmrChanges[i].Changes[j].ReplayPlayerId;
                parameters[4].Value = replayPlayerMmrChanges[i].ReplayId;
                await command.ExecuteNonQueryAsync();
            }
        }
        await transaction.CommitAsync();
        return appendId;
    }

    private async Task DeleteReplayPlayerRatingsTable()
    {
        using var connection = new SqliteConnection(Data.SqliteConnectionString);
        await connection.OpenAsync();

        using var delCommand = new SqliteCommand("DELETE FROM ReplayPlayerRatings;", connection);
        await delCommand.ExecuteNonQueryAsync();
    }
}
