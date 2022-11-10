using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;
public partial class MmrService
{
    public async Task<Dictionary<int, PlayerInfoDto>> GetPlayerInfos()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playerInfosStd = await GetPlayerInfos(context, std: true);
        var teamGamesStd = await GetPlayerTeamGames(context, std: true);

        var playerInfosCmdr = await GetPlayerInfos(context, std: false);
        var teamGamesCmdr = await GetPlayerTeamGames(context, std: false);


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
        return infos;
    }

    private async Task<List<PlayerInfo>> GetPlayerInfos(ReplayContext context, bool std)
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

    private async Task<Dictionary<int, int>> GetPlayerTeamGames(ReplayContext context, bool std)
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
