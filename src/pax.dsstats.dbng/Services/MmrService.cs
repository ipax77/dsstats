using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace pax.dsstats.dbng.Services;

public class MmrService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<MmrService> logger;

    public MmrService(IServiceProvider serviceProvider, IMapper mapper, ILogger<MmrService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }
    private readonly int eloK = 35; // default 35
    private readonly double startMmr = 1000.0;
    private readonly Dictionary<int, List<DsRCheckpoint>> ratings = new();
    private readonly Dictionary<int, List<DsRCheckpoint>> ratingsstd = new();

    public event EventHandler<EventArgs>? Recalculated;
    protected virtual void OnRecalculated(EventArgs e)
    {
        EventHandler<EventArgs>? handler = Recalculated;
        handler?.Invoke(this, e);
    }

    public async Task CalcMmmr()
    {
        Stopwatch sw = new();
        sw.Start();

        await SeedCommanderMmrs();

        await ClearRatings();

        await CalcMmrCmdr();
        await CalcMmrStd();

        sw.Stop();
        logger.LogWarning($"ratings calculated in {sw.ElapsedMilliseconds} ms");

        OnRecalculated(new());
    }

    private async Task SeedCommanderMmrs()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (!context.CommanderMmrs.Any())
        {
            foreach (Commander cmdr in Data.GetCommanders(Data.CmdrGet.NoNone))
            {
                foreach (Commander synCmdr in Data.GetCommanders(Data.CmdrGet.NoNone))
                {
                    context.CommanderMmrs.Add(new()
                    {
                        Commander = cmdr,
                        SynCommander = synCmdr
                    });
                }
            }
        }
        await context.SaveChangesAsync();
    }

    private async Task CalcMmrStd()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();



        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => x.Duration >= 300 && x.Maxleaver < 89 && x.WinnerTeam > 0)
            .Where(x => x.Playercount == 6 && x.GameMode == GameMode.Standard)
            .OrderBy(o => o.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider);


        await replays.ForEachAsync(f =>
        {
            var winnerTeam = f.ReplayPlayers.Where(x => x.Team == f.WinnerTeam).Select(m => m.Player);
            var runnerTeam = f.ReplayPlayers.Where(x => x.Team != f.WinnerTeam).Select(m => m.Player);

            var winnerTeamMmr = GetTeamMmrStd(winnerTeam, f.GameTime);
            var runnerTeamMmr = GetTeamMmrStd(runnerTeam, f.GameTime);

            var delta = CalculateELODelta(winnerTeamMmr, runnerTeamMmr);

            SetWinnerMmrStd(winnerTeam, delta, f.GameTime);
            SetRunnerMmrStd(runnerTeam, delta, f.GameTime);
        });

        await SetRatingsStd();
    }

    private async Task CalcMmrCmdr()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => x.Duration >= 300 && x.Maxleaver < 89 && x.WinnerTeam > 0)
            .Where(x => x.Playercount == 6 && (x.GameMode == GameMode.Commanders || x.GameMode == GameMode.CommandersHeroic))
            .OrderBy(o => o.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider);


        await replays.ForEachAsync(f =>
        {
            var winnerTeam = f.ReplayPlayers.Where(x => x.Team == f.WinnerTeam).Select(m => m.Player);
            var runnerTeam = f.ReplayPlayers.Where(x => x.Team != f.WinnerTeam).Select(m => m.Player);

            var winnerTeamMmr = GetTeamMmr(winnerTeam, f.GameTime);
            var runnerTeamMmr = GetTeamMmr(runnerTeam, f.GameTime);

            var delta = CalculateELODelta(winnerTeamMmr, runnerTeamMmr);

            SetWinnerMmr(winnerTeam, delta, f.GameTime);
            SetRunnerMmr(runnerTeam, delta, f.GameTime);
        });

        await SetRatings();
    }

    private async Task SetRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int i = 0;
        foreach (var ent in ratings)
        {
            var player = await context.Players.FirstAsync(f => f.PlayerId == ent.Key);
            player.Mmr = ent.Value.Last().Mmr;
            player.MmrOverTime = GetOverTimeRating(ent.Value);
            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();
    }

    private async Task SetRatingsStd()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int i = 0;
        foreach (var ent in ratingsstd)
        {
            var player = await context.Players.FirstAsync(f => f.PlayerId == ent.Key);
            player.MmrStd = ent.Value.Last().MmrStd;
            player.MmrStdOverTime = GetOverTimeRatingStd(ent.Value);
            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();
    }

    private static string? GetOverTimeRating(List<DsRCheckpoint> dsRCheckpoints)
    {
        if (dsRCheckpoints.Count == 0)
        {
            return null;
        }

        else if (dsRCheckpoints.Count == 1)
        {
            return $"{Math.Round(dsRCheckpoints[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[0].Time:MMyy}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(dsRCheckpoints.First().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.First().Time:MMyy}");

        if (dsRCheckpoints.Count > 2)
        {
            string timeStr = dsRCheckpoints[0].Time.ToString(@"MMyy");
            for (int i = 1; i < dsRCheckpoints.Count - 1; i++)
            {
                string currentTimeStr = dsRCheckpoints[i].Time.ToString(@"MMyy");
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(dsRCheckpoints[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[i].Time:MMyy}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(dsRCheckpoints.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.Last().Time:MMyy}");

        if (sb.Length > 1999)
        {
            throw new ArgumentOutOfRangeException(nameof(dsRCheckpoints));
        }

        return sb.ToString();
    }

    private static string? GetOverTimeRatingStd(List<DsRCheckpoint> dsRCheckpoints)
    {
        if (dsRCheckpoints.Count == 0)
        {
            return null;
        }

        else if (dsRCheckpoints.Count == 1)
        {
            return $"{Math.Round(dsRCheckpoints[0].MmrStd, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[0].Time:MMyy}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(dsRCheckpoints.First().MmrStd, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.First().Time:MMyy}");

        if (dsRCheckpoints.Count > 2)
        {
            string timeStr = dsRCheckpoints[0].Time.ToString(@"MMyy");
            for (int i = 1; i < dsRCheckpoints.Count - 1; i++)
            {
                string currentTimeStr = dsRCheckpoints[i].Time.ToString(@"MMyy");
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(dsRCheckpoints[i].MmrStd, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[i].Time:MMyy}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(dsRCheckpoints.Last().MmrStd, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.Last().Time:MMyy}");

        if (sb.Length > 1999)
        {
            throw new ArgumentOutOfRangeException(nameof(dsRCheckpoints));
        }

        return sb.ToString();
    }

    private async Task ClearRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // todo: db-lock (no imports possible during this)
        await context.Database.ExecuteSqlRawAsync($"UPDATE Players SET Mmr = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE Players SET MmrStd = {startMmr}");
        await context.Database.ExecuteSqlRawAsync("UPDATE Players SET MmrOverTime = NULL");
        await context.Database.ExecuteSqlRawAsync("UPDATE Players SET MmrStdOverTime = NULL");
        ratings.Clear();
        ratingsstd.Clear();
    }

    private void SetRunnerMmr(IEnumerable<PlayerDsRDto> teamPlayers, double delta, DateTime gameTime)
    {
        foreach (var player in teamPlayers)
        {
            var plRatings = ratings[player.PlayerId];
            var newRating = plRatings.Last().Mmr - delta;
            plRatings.Add(new DsRCheckpoint() { Mmr = newRating, Time = gameTime });
        }
    }

    private void SetWinnerMmr(IEnumerable<PlayerDsRDto> teamPlayers, double delta, DateTime gameTime)
    {
        foreach (var player in teamPlayers)
        {
            var plRatings = ratings[player.PlayerId];
            var newRating = plRatings.Last().Mmr + delta;
            plRatings.Add(new DsRCheckpoint() { Mmr = newRating, Time = gameTime });
        }
    }

    private void SetRunnerMmrStd(IEnumerable<PlayerDsRDto> teamPlayers, double delta, DateTime gameTime)
    {
        foreach (var player in teamPlayers)
        {
            var plRatings = ratingsstd[player.PlayerId];
            var newRating = plRatings.Last().MmrStd - delta;
            plRatings.Add(new DsRCheckpoint() { MmrStd = newRating, Time = gameTime });
        }
    }

    private void SetWinnerMmrStd(IEnumerable<PlayerDsRDto> teamPlayers, double delta, DateTime gameTime)
    {
        foreach (var player in teamPlayers)
        {
            var plRatings = ratingsstd[player.PlayerId];
            var newRating = plRatings.Last().MmrStd + delta;
            plRatings.Add(new DsRCheckpoint() { MmrStd = newRating, Time = gameTime });
        }
    }

    private static double ExpectationToWin(double playerOneRating, double playerTwoRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (playerTwoRating - playerOneRating) / 400.0));
    }

    private double CalculateELODelta(double winnerTeamMmr, double runnerTeamMmr)
    {

        return (double)(eloK * (1.0 - ExpectationToWin(winnerTeamMmr, runnerTeamMmr)));
    }

    private double GetTeamMmr(IEnumerable<PlayerDsRDto> players, DateTime gameTime)
    {
        double teamMmr = 0;

        foreach (var player in players)
        {
            if (!ratings.ContainsKey(player.PlayerId))
            {
                ratings[player.PlayerId] = new List<DsRCheckpoint>() { new() { Mmr = 1000.0, Time = gameTime } };
                teamMmr += 1000.0;
            }
            else
            {
                teamMmr += ratings[player.PlayerId].Last().Mmr;
            }
        }
        return teamMmr / 3.0;
    }

    private double GetTeamMmrStd(IEnumerable<PlayerDsRDto> players, DateTime gameTime)
    {
        double teamMmr = 0;

        foreach (var player in players)
        {
            if (!ratingsstd.ContainsKey(player.PlayerId))
            {
                ratingsstd[player.PlayerId] = new List<DsRCheckpoint>() { new() { MmrStd = 1000.0, Time = gameTime } };
                teamMmr += 1000.0;
            }
            else
            {
                teamMmr += ratingsstd[player.PlayerId].Last().Mmr;
            }
        }
        return teamMmr / 3.0;
    }
}

public record DsRCheckpoint
{
    public double Consistency { get; init; }
    public double Mmr { get; init; }
    public double MmrStd { get; init; }
    public DateTime Time { get; init; }
}
