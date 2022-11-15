﻿
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Diagnostics;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    public async Task<bool> ContinueCalculateWithDictionary(List<Replay> newReplays)
    {
        if (newReplays.Any(x => x.GameTime < LatestReplayGameTime))
        {
            //ReCalculateWithDictionary(startTime, DateTime.Today.AddDays(1));
            return false;
        }

        var newReplaysCmdr = newReplays
            .Where(x =>
                x.Playercount == 6
                && x.Duration >= 300
                && x.WinnerTeam > 0
                && (x.GameMode == GameMode.Commanders || x.GameMode == GameMode.CommandersHeroic))
            .OrderBy(o => o.GameTime)
                .ThenBy(t => t.ReplayId)
            .Select(s => mapper.Map<ReplayDsRDto>(s)).ToList();

        var newReplaysStd = newReplays
            .Where(x =>
                x.Playercount == 6
                && x.Duration >= 300
                && x.WinnerTeam > 0
                && x.GameMode == GameMode.Standard)
            .OrderBy(o => o.GameTime)
                .ThenBy(t => t.ReplayId)
            .Select(s => mapper.Map<ReplayDsRDto>(s)).ToList();

        await ss.WaitAsync();
        try
        {
            Stopwatch sw = Stopwatch.StartNew();

            var playerRatingsCmdr = GetPlayerRatingsCmdr(newReplaysCmdr);
            var playerRatingsStd = GetPlayerRatingsStd(newReplaysStd);
            playerRatingsCmdr = ContinueCalculateCmdr(playerRatingsCmdr, newReplaysCmdr);
            playerRatingsStd = ContinueCalculateStd(playerRatingsStd, newReplaysStd);

            // todo: optimize
            var playerInfos = await GetPlayerInfos();

            await ContinueGlobals(playerRatingsCmdr, playerRatingsStd, playerInfos);

            await SaveCommanderData();

            sw.Stop();
            logger.LogInformation($"continue calculation in {sw.ElapsedMilliseconds} ms");
            OnRecalculated(new() { Duration = sw.Elapsed });
        }
        finally
        {
            ss.Release();
        }

        return true;
    }

    private Dictionary<int, List<DsRCheckpoint>> GetPlayerRatingsCmdr(List<ReplayDsRDto> newReplaysCmdr)
    {
        Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr = new();

        if (!newReplaysCmdr.Any())
        {
            return playerRatingsCmdr;
        }

        foreach (var rp in newReplaysCmdr.SelectMany(s => s.ReplayPlayers).ToList())
        {
            if (ToonIdRatings.ContainsKey(rp.Player.ToonId))
            {
                var rpCp = ToonIdRatings[rp.Player.ToonId];
                playerRatingsCmdr[GetMmrId(rp.Player)] = new List<DsRCheckpoint>()
                {
                    new DsRCheckpoint()
                    {
                        Consistency = rpCp.CmdrRatingStats.Consistency,
                        Mmr = rpCp.CmdrRatingStats.Mmr,
                        Time = DateTime.Today
                    }
                };
            }
        }
        return playerRatingsCmdr;
    }

    private Dictionary<int, List<DsRCheckpoint>> GetPlayerRatingsStd(List<ReplayDsRDto> newReplaysStd)
    {
        Dictionary<int, List<DsRCheckpoint>> playerRatingsStd = new();

        if (!newReplaysStd.Any())
        {
            return playerRatingsStd;
        }

        foreach (var rp in newReplaysStd.SelectMany(s => s.ReplayPlayers).ToList())
        {
            if (ToonIdRatings.ContainsKey(rp.Player.ToonId))
            {
                var rpCp = ToonIdRatings[rp.Player.ToonId];
                playerRatingsStd[rp.Player.ToonId] = new List<DsRCheckpoint>()
                {
                    new DsRCheckpoint()
                    {
                        Consistency = rpCp.StdRatingStats.Consistency,
                        Mmr = rpCp.StdRatingStats.Mmr,
                        Time = DateTime.Today
                    }
                };
            }
        }
        return playerRatingsStd;
    }

    private async Task ContinueGlobals(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr,
                               Dictionary<int, List<DsRCheckpoint>> playerRatingsStd,
                               Dictionary<int, PlayerInfoDto> playerInfos)
    {
        // todo: optimize for continue
        var playerIdToonIdMap = await GetPlayerIdToonIdMap();

        foreach (var playerRatingEnt in playerRatingsCmdr)
        {
            int playerId = playerRatingEnt.Key;

            int toonId = 0;
            string name = "";

            if (playerIdToonIdMap.ContainsKey(playerId))
            {
                var playerMap = playerIdToonIdMap[playerId];
                toonId = playerMap.Key;
                name = playerMap.Value;
            }

            PlayerInfoDto playerInfo;
            if (playerInfos.ContainsKey(toonId))
            {
                playerInfo = playerInfos[toonId];
            }
            else
            {
                playerInfo = new PlayerInfoDto()
                {
                    ToonId = toonId
                };
                playerInfos[toonId] = playerInfo;
            }

            MmrInfo? mmrInfoCmdr = null;
            if (playerId > 0 && playerRatingsCmdr.ContainsKey(playerId))
            {
                var plRat = playerRatingsCmdr[playerId];
                var lastPlRat = plRat.LastOrDefault();
                mmrInfoCmdr = new()
                {
                    Mmr = lastPlRat?.Mmr ?? startMmr,
                    Consistency = lastPlRat?.Consistency ?? 0
                };
                ToonIdCmdrRatingOverTime[toonId] = ContinueOverTimeRatingCmdr(toonId, plRat) ?? "";
            }

            MmrInfo? mmrInfoStd = null;
            if (playerId > 0 && playerRatingsStd.ContainsKey(playerId))
            {
                var plRat = playerRatingsStd[playerId];
                var lastPlRat = plRat.LastOrDefault();
                mmrInfoStd = new()
                {
                    Mmr = lastPlRat?.Mmr ?? startMmr,
                    Consistency = lastPlRat?.Consistency ?? 0
                };
                ToonIdStdRatingOverTime[toonId] = ContinueOverTimeRatingStd(toonId, plRat) ?? "";
            }

            if (mmrInfoCmdr == null && mmrInfoStd == null)
            {
                continue;
            }

            if (ToonIdRatings.ContainsKey(toonId))
            {
                var toonIdRating = ToonIdRatings[toonId];
                toonIdRating.CmdrRatingStats.Mmr = mmrInfoCmdr?.Mmr ?? toonIdRating.CmdrRatingStats.Mmr;
                toonIdRating.CmdrRatingStats.Games = playerInfo.GamesCmdr;
                toonIdRating.CmdrRatingStats.Wins = playerInfo.WinsCmdr;
                toonIdRating.CmdrRatingStats.Mvp = playerInfo.MvpCmdr;
                toonIdRating.CmdrRatingStats.TeamGames = playerInfo.TeamGamesCmdr;
                toonIdRating.CmdrRatingStats.Consistency = mmrInfoCmdr?.Consistency ?? toonIdRating.CmdrRatingStats.Consistency;

                toonIdRating.StdRatingStats.Mmr = mmrInfoStd?.Mmr ?? toonIdRating.StdRatingStats.Mmr;
                toonIdRating.StdRatingStats.Games = playerInfo.GamesStd;
                toonIdRating.StdRatingStats.Wins = playerInfo.WinsStd;
                toonIdRating.StdRatingStats.Mvp = playerInfo.MvpStd;
                toonIdRating.StdRatingStats.TeamGames = playerInfo.TeamGamesStd;
                toonIdRating.StdRatingStats.Consistency = mmrInfoStd?.Consistency ?? toonIdRating.StdRatingStats.Consistency;
            }
            else
            {
                ToonIdRatings[toonId] = new PlayerRatingDto()
                {
                    PlayerId = playerId,
                    Name = name,
                    ToonId = toonId,

                    CmdrRatingStats = new()
                    {
                        Mmr = mmrInfoCmdr?.Mmr ?? startMmr,
                        Games = playerInfo.GamesCmdr,
                        Wins = playerInfo.WinsCmdr,
                        Mvp = playerInfo.MvpCmdr,
                        TeamGames = playerInfo.TeamGamesCmdr,
                        Consistency = mmrInfoCmdr?.Consistency ?? 0
                    },
                    StdRatingStats = new()
                    {
                        Mmr = mmrInfoStd?.Mmr ?? startMmr,
                        Games = playerInfo.GamesStd,
                        Wins = playerInfo.WinsStd,
                        Mvp = playerInfo.MvpStd,
                        TeamGames = playerInfo.TeamGamesStd,
                        Consistency = mmrInfoCmdr?.Consistency ?? 0
                    }
                };
            }

        }
    }

    private async Task ContinueGlobals_deprecated(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr,
                               Dictionary<int, List<DsRCheckpoint>> playerRatingsStd,
                               Dictionary<int, PlayerInfoDto> playerInfos)
    {
        // todo: optimize for continue
        var toonIdPlayerIdMap = await GetToonIdPlayerIdMap();

        for (int i = 0; i < playerInfos.Count; i++)
        {
            var playerInfo = playerInfos.ElementAt(i);
            int toonId = playerInfo.Key;
            int playerId = 0;
            string name = "";


            if (toonIdPlayerIdMap.ContainsKey(toonId))
            {
                var tpMap = toonIdPlayerIdMap[toonId];
                playerId = tpMap.Key;
                name = tpMap.Value;
            }

            MmrInfo? mmrInfoCmdr = null;
            if (playerId > 0 && playerRatingsCmdr.ContainsKey(playerId))
            {
                var plRat = playerRatingsCmdr[playerId];
                var lastPlRat = plRat.LastOrDefault();
                mmrInfoCmdr = new()
                {
                    Mmr = lastPlRat?.Mmr ?? startMmr,
                    Consistency = lastPlRat?.Consistency ?? 0
                };
                ToonIdCmdrRatingOverTime[toonId] = ContinueOverTimeRatingCmdr(toonId, plRat) ?? "";
            }

            MmrInfo? mmrInfoStd = null;
            if (playerId > 0 && playerRatingsStd.ContainsKey(playerId))
            {
                var plRat = playerRatingsStd[playerId];
                var lastPlRat = plRat.LastOrDefault();
                mmrInfoStd = new()
                {
                    Mmr = lastPlRat?.Mmr ?? startMmr,
                    Consistency = lastPlRat?.Consistency ?? 0
                };
                ToonIdStdRatingOverTime[toonId] = ContinueOverTimeRatingStd(toonId, plRat) ?? "";
            }

            if (mmrInfoCmdr == null && mmrInfoStd == null)
            {
                continue;
            }

            if (ToonIdRatings.ContainsKey(toonId))
            {
                var toonIdRating = ToonIdRatings[toonId];
                toonIdRating.CmdrRatingStats.Mmr = mmrInfoCmdr?.Mmr ?? toonIdRating.CmdrRatingStats.Mmr;
                toonIdRating.CmdrRatingStats.Games += playerInfo.Value.GamesCmdr;
                toonIdRating.CmdrRatingStats.Wins += playerInfo.Value.WinsCmdr;
                toonIdRating.CmdrRatingStats.Mvp += playerInfo.Value.MvpCmdr;
                toonIdRating.CmdrRatingStats.TeamGames += playerInfo.Value.TeamGamesCmdr;
                toonIdRating.CmdrRatingStats.Consistency = mmrInfoCmdr?.Consistency ?? toonIdRating.CmdrRatingStats.Consistency;

                toonIdRating.StdRatingStats.Mmr = mmrInfoStd?.Mmr ?? toonIdRating.StdRatingStats.Mmr;
                toonIdRating.StdRatingStats.Games += playerInfo.Value.GamesStd;
                toonIdRating.StdRatingStats.Wins += playerInfo.Value.WinsStd;
                toonIdRating.StdRatingStats.Mvp += playerInfo.Value.MvpStd;
                toonIdRating.StdRatingStats.TeamGames += playerInfo.Value.TeamGamesStd;
                toonIdRating.StdRatingStats.Consistency = mmrInfoStd?.Consistency ?? toonIdRating.StdRatingStats.Consistency;
            }
            else
            {
                ToonIdRatings[toonId] = new PlayerRatingDto()
                {
                    PlayerId = playerId,
                    Name = name,
                    ToonId = toonId,

                    CmdrRatingStats = new()
                    {
                        Mmr = mmrInfoCmdr?.Mmr ?? startMmr,
                        Games = playerInfo.Value.GamesCmdr,
                        Wins = playerInfo.Value.WinsCmdr,
                        Mvp = playerInfo.Value.MvpCmdr,
                        TeamGames = playerInfo.Value.TeamGamesCmdr,
                        Consistency = mmrInfoCmdr?.Consistency ?? 0
                    },
                    StdRatingStats = new()
                    {
                        Mmr = mmrInfoStd?.Mmr ?? startMmr,
                        Games = playerInfo.Value.GamesStd,
                        Wins = playerInfo.Value.WinsStd,
                        Mvp = playerInfo.Value.MvpStd,
                        TeamGames = playerInfo.Value.TeamGamesStd,
                        Consistency = mmrInfoCmdr?.Consistency ?? 0
                    }
                };
            }
        }
    }

    private static string? ContinueOverTimeRating(string? currentOtr, List<DsRCheckpoint> dsRCheckpoints)
    {
        string? continueOtr = GetOverTimeRating(dsRCheckpoints);

        if (string.IsNullOrEmpty(continueOtr))
        {
            return currentOtr;
        }
        else
        {
            return currentOtr + '|' + continueOtr;
        }
    }

    private string? ContinueOverTimeRatingCmdr(int toonId, List<DsRCheckpoint> dsRCheckpoints)
    {
        if (ToonIdCmdrRatingOverTime.ContainsKey(toonId))
        {
            string currentOtr = ToonIdCmdrRatingOverTime[toonId];
            return ContinueOverTimeRating(currentOtr, dsRCheckpoints);
        }
        else
        {
            return GetOverTimeRating(dsRCheckpoints);
        }
    }

    private string? ContinueOverTimeRatingStd(int toonId, List<DsRCheckpoint> dsRCheckpoints)
    {
        if (ToonIdStdRatingOverTime.ContainsKey(toonId))
        {
            string currentOtr = ToonIdCmdrRatingOverTime[toonId];
            return ContinueOverTimeRating(currentOtr, dsRCheckpoints);
        }
        else
        {
            return GetOverTimeRating(dsRCheckpoints);
        }
    }
}