using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using System.Diagnostics;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(int toonId)
    {
        return await GetPlayerDetailInfo(new List<int>() { toonId });
    }

    public async Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(List<int> toonIds)
    {
        return await GetPlayerMatchups(toonIds);

        //return new PlayerDetailInfo()
        //{
        //    MatchupInfos = matchups
        //};
    }

    private async Task<List<PlayerMatchupInfo>> GetPlayerMatchups(List<int> toonIds)
    {
        var countGroup = from p in context.Players
                         from rp in p.ReplayPlayers
                         where toonIds.Contains(p.ToonId)
                         group rp by new { rp.Race, rp.OppRace } into g
                         select new PlayerMatchupInfo
                         {
                             Commander = g.Key.Race,
                             Versus = g.Key.OppRace,
                             Count = g.Count(),
                             Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                         };

        var matchups = await countGroup.ToListAsync();
        return matchups
            .Where(x => x.Commander > 0 && x.Versus > 0)
            .ToList();
    }

    private List<PlayerCmdrInfo> GetPlayerCmdrInfos(List<PlayerMatchupInfo> matchups)
    {
        var cmdrInfos = from m in matchups
                        group m by m.Commander into g
                        select new PlayerCmdrInfo
                        {
                            Commander = g.Key,
                            Count = g.Sum(s => s.Count),
                            Wins = g.Sum(s => s.Wins),
                        };
        return cmdrInfos.ToList();
    }

    public async Task SeedPlayerInfos()
    {
        Stopwatch sw = new();
        sw.Start();

        var playerInfosStd = await GetPlayerInfos(std: true);
        var teamGamesStd = await GetPlayerTeamGames(std: true);

        var playerInfosCmdr = await GetPlayerInfos(std: false);
        var teamGamesCmdr = await GetPlayerTeamGames(std: false);


        var infos = playerInfosCmdr.ToDictionary(k => k.ToonId, v => new PlayerInfoDto()
        {
            GamesCmdr = v.Games,
            WinsCmdr = v.Wins,
            MvpCmdr = v.Mvp,
            TeamGamesCmdr = teamGamesCmdr.ContainsKey(v.ToonId) ? teamGamesCmdr[v.ToonId] : 0
        });

        foreach (var stdInfo in playerInfosStd)
        {
            if (infos.ContainsKey(stdInfo.ToonId))
            {
                infos[stdInfo.ToonId].GamesStd = stdInfo.Games;
                infos[stdInfo.ToonId].WinsStd = stdInfo.Wins;
                infos[stdInfo.ToonId].MvpStd = stdInfo.Mvp;
                infos[stdInfo.ToonId].TeamGamesStd = teamGamesStd.ContainsKey(stdInfo.ToonId) ? teamGamesStd[stdInfo.ToonId] : 0;

            }
            else
            {
                infos[stdInfo.ToonId] = new PlayerInfoDto()
                {
                    GamesStd = stdInfo.Games,
                    WinsStd = stdInfo.Wins,
                    MvpStd = stdInfo.Mvp,
                    TeamGamesStd = teamGamesStd.ContainsKey(stdInfo.ToonId) ? teamGamesStd[stdInfo.ToonId] : 0
                };
            }
        }
        sw.Stop();

        Console.WriteLine($"player infos created in {sw.ElapsedMilliseconds} ms ({infos.Count})");

        sw.Restart();

        int i = 0;
        foreach (var info in infos)
        {
            var player = await context.Players.FirstOrDefaultAsync(f => f.ToonId == info.Key);
            if (player == null)
            {
                continue;
            }
            else
            {
                player.GamesStd = info.Value.GamesStd;
                player.WinsStd = info.Value.WinsStd;
                player.MvpStd = info.Value.MvpStd;
                player.TeamGamesStd = info.Value.TeamGamesStd;

                player.GamesCmdr = info.Value.GamesCmdr;
                player.WinsCmdr = info.Value.WinsCmdr;
                player.MvpCmdr = info.Value.MvpCmdr;
                player.TeamGamesCmdr = info.Value.TeamGamesCmdr;
            }
            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();

        sw.Stop();
        Console.WriteLine($"player infos db update in {sw.ElapsedMilliseconds} ms");
    }

    private async Task<List<PlayerInfo>> GetPlayerInfos(bool std)
    {
        var infos = std ?
                     from r in context.Replays
                     from rp in r.ReplayPlayers
                     where r.DefaultFilter && r.GameMode == GameMode.Standard
                     group rp by rp.Player.ToonId into g
                     select new PlayerInfo
                     {
                         ToonId = g.Key,
                         Games = g.Count(),
                         Wins = g.Count(c => c.PlayerResult == PlayerResult.Win),
                         Mvp = g.Count(c => c.Kills == c.Replay.Maxkillsum)
                     }
                    : from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.DefaultFilter && new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic }.Contains(r.GameMode)
                    group rp by rp.Player.ToonId into g
                    select new PlayerInfo
                    {
                        ToonId = g.Key,
                        Games = g.Count(),
                        Wins = g.Count(c => c.PlayerResult == PlayerResult.Win),
                        Mvp = g.Count(c => c.Kills == c.Replay.Maxkillsum)
                    };

        return await infos.ToListAsync();
    }

    private async Task<Dictionary<int, int>> GetPlayerTeamGames(bool std)
    {
        var teamGroup = std ?
                        from r in context.Replays
                          from rp1 in r.ReplayPlayers
                          where r.DefaultFilter && r.GameMode == GameMode.Standard
                          join rp2 in context.ReplayPlayers
                            on new { Id = r.ReplayId, rp1.Team }
                            equals new { Id = rp2.ReplayId, rp2.Team }
                          where rp2.ReplayPlayerId != rp1.ReplayPlayerId
                          group new { rp1, rp2 } by rp1.Player.ToonId into g
                          select new
                          {
                              ToonId = g.Key,
                              Teamgames = g.Count(c => c.rp2.IsUploader)
                          }
                        : from r in context.Replays
                        from rp1 in r.ReplayPlayers
                        where r.DefaultFilter && new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic }.Contains(r.GameMode)
                          join rp2 in context.ReplayPlayers
                            on new { Id = r.ReplayId, rp1.Team }
                            equals new { Id = rp2.ReplayId, rp2.Team }
                        where rp2.ReplayPlayerId != rp1.ReplayPlayerId
                        group new { rp1, rp2 } by rp1.Player.ToonId into g
                        select new
                        {
                            ToonId = g.Key,
                            Teamgames = g.Count(c => c.rp2.IsUploader)
                        };

        var teamGames = await teamGroup.ToListAsync();
        return teamGames.ToDictionary(k => k.ToonId, v => v.Teamgames);
    }

}

public record PlayerInfo
{
    public int ToonId { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
}

public record PlayerInfoDto
{
    public int ToonId { get; set; }
    public int GamesCmdr { get; set; }
    public int WinsCmdr { get; set; }
    public int MvpCmdr { get; set; }
    public int TeamGamesCmdr { get; set; }
    public int GamesStd { get; set; }
    public int WinsStd { get; set; }
    public int MvpStd { get; set; }
    public int TeamGamesStd { get; set; }
}