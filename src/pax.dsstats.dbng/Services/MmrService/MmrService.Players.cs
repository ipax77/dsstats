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

        var playerInfosCmdr = await GetPlayerInfos(context, std: false);

        var mainCmdrs = await GetMains(context);

        var infos = playerInfosCmdr.ToDictionary(k => k.ToonId, v => new PlayerInfoDto()
        {
            GamesCmdr = v.Games,
            WinsCmdr = v.Wins,
            MvpCmdr = v.Mvp,
        });

        foreach (var stdInfo in playerInfosStd)
        {
            if (infos.ContainsKey(stdInfo.ToonId))
            {
                infos[stdInfo.ToonId].GamesStd = stdInfo.Games;
                infos[stdInfo.ToonId].WinsStd = stdInfo.Wins;
                infos[stdInfo.ToonId].MvpStd = stdInfo.Mvp;

            }
            else
            {
                infos[stdInfo.ToonId] = new PlayerInfoDto()
                {
                    GamesStd = stdInfo.Games,
                    WinsStd = stdInfo.Wins,
                    MvpStd = stdInfo.Mvp,
                };
            }
        }

        foreach (var mainCmdr in mainCmdrs)
        {
            if (infos.ContainsKey(mainCmdr.ToonId))
            {
                infos[mainCmdr.ToonId].Main = mainCmdr.Commander;
                infos[mainCmdr.ToonId].MainPercentage = mainCmdr.PlayedPercentage;
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
                     where rp.Player.RegionId >= 0 && rp.Player.Name != "Anonymouse"
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
                      where rp.Player.RegionId >= 0 && rp.Player.Name != "Anonymouse"
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

    private async Task<List<MainInfo>> GetMains(ReplayContext context)
    {
        //var players = GetInfoPlayersQueriable(context);

        var mainQuery = from p in context.Players
                   from rp in p.ReplayPlayers
                   group new { p, rp } by new { p.ToonId, rp.Race } into g
                   select new
                   {
                       ToonId = g.Key.ToonId,
                       Race = g.Key.Race,
                       Count = g.Count()
                   };

        var toonQuery = from m in mainQuery
                        where m.Count > 20
                        select m;

        var mainCounts = await mainQuery.ToListAsync();

        var mainGroup = mainCounts.GroupBy(g => g.ToonId)
            .Select(s => new
            {
                ToonId = s.Key,
                Sum = s.Sum(t => t.Count),
                Max = s.Max(m => m.Count),
                Main = s.First(f => f.Count == s.Max(m => m.Count)).Race
            });


        List<MainInfo> mainInfos = new();

        foreach (var ent in mainGroup)
        {
            mainInfos.Add(new()
            {
                ToonId = ent.ToonId,
                Commander = ent.Main,
                PlayedPercentage = MathF.Round(ent.Max * 100.0f / (float)ent.Sum, 2)

            });
        }
        return mainInfos;
    }

    private IQueryable<Player> GetInfoPlayersQueriable(ReplayContext context)
    {
        var players = from p in context.Players.Include(i => i.ReplayPlayers)
                      from rp in p.ReplayPlayers
                      where p.RegionId >= 0 && p.Name != "Anonymouse"
                      select p;

        return players;
    }
}

public record MainInfo
{
    public int ToonId { get; set; }
    public Commander Commander { get; set; }
    public float PlayedPercentage { get; set; }
}