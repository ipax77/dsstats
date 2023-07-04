
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace pax.dsstats.dbng.Extensions;

public static class ReplayExtensions
{
    public static void SetDefaultFilter(this Replay replay)
    {
        if (replay.Playercount == 6
            && replay.Duration >= 300
            && replay.Maxleaver < 90
            && replay.WinnerTeam > 0)
        {
            replay.DefaultFilter = true;
        }
    }

    public static string GenHash(this Replay replay)
    {
        if (!replay.ReplayPlayers.Any())
        {
            throw new ArgumentOutOfRangeException(nameof(replay));
        }

        StringBuilder sb = new();
        foreach (var pl in replay.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            if (pl.Player == null)
            {
                throw new ArgumentOutOfRangeException(nameof(replay));
            }
            sb.Append(pl.GamePos + pl.Race + pl.Player.ToonId);
        }
        sb.Append(replay.GameMode + replay.Playercount);
        sb.Append(replay.Minarmy + replay.Minkillsum + replay.Minincome + replay.Maxkillsum);

        using var md5Hash = MD5.Create();
        return GetMd5Hash(md5Hash, sb.ToString());
    }

    public static string GenHash(this Spawn spawn, Replay replay)
    {
        StringBuilder sb = new();

        foreach (var pl in replay.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            //if (pl.Player == null)
            //{
            //    throw new ArgumentOutOfRangeException(nameof(replay));
            //}
            //sb.Append(pl.GamePos + pl.Race + pl.Player.ToonId);
            sb.Append(pl.GamePos + pl.Race + pl.PlayerId);
        }

        sb.Append(spawn.Gameloop);
        sb.Append(spawn.Income);
        sb.Append(spawn.GasCount);
        sb.Append(spawn.ArmyValue);
        sb.Append(spawn.KilledValue);
        sb.Append(spawn.UpgradeSpent);

        sb.Append(string.Concat(spawn.Units.Select(s => $"{s.UnitId}{s.Poss}")));

        using var md5Hash = MD5.Create();
        return GetMd5Hash(md5Hash, sb.ToString());
    }

    public static string GenCountMemKey(this StatsRequest statsRequest)
    {
        var sb = new StringBuilder();
        sb.Append("StatsCount");
        sb.Append((int)statsRequest.Interest);
        sb.Append(statsRequest.TimePeriod);
        sb.Append(String.Concat(statsRequest.GameModes.Select(s => (int)s)));
        sb.Append(statsRequest.TeMaps);
        if (statsRequest.PlayerNames.Any())
        {
            sb.Append(string.Join('x', statsRequest.PlayerNames.Select(s => s.ToonId.ToString())));
        }
        return sb.ToString();
    }

    public static string GenStatsMemKey(this StatsRequest statsRequest)
    {
        var sb = new StringBuilder();
        if (statsRequest.StatsMode == StatsMode.Duration || statsRequest.StatsMode == StatsMode.Timeline || statsRequest.StatsMode == StatsMode.Synergy)
        {
            sb.Append("Lr");
        }
        sb.Append("Stats");
        sb.Append(statsRequest.StatsMode.ToString());
        sb.Append((int)statsRequest.Interest);
        sb.Append(statsRequest.DefaultFilter);
        sb.Append(statsRequest.Uploaders);
        sb.Append(statsRequest.TimePeriod);
        sb.Append(statsRequest.TeMaps);
        if (statsRequest.PlayerNames.Any())
        {
            sb.Append(string.Join('x', statsRequest.PlayerNames.Select(s => s.ToonId.ToString())));
        }
        sb.Append(String.Concat(statsRequest.GameModes.Select(s => (int)s)));
        return sb.ToString();
    }

    public static string GenMemKey(this BuildRequest buildRequest)
    {
        var sb = new StringBuilder();
        sb.Append("Builds");
        sb.Append(buildRequest.Timespan);
        sb.Append(buildRequest.Interest);
        sb.Append(buildRequest.Versus);
        sb.Append(String.Concat(buildRequest.PlayerNames.Select(s => s.ToonId.ToString())));
        return sb.ToString();
    }

    public static string GenMemKey(this CmdrRequest cmdrRequest)
    {
        var sb = new StringBuilder();
        sb.Append("Cmdr");
        sb.Append(cmdrRequest.TimeSpan);
        sb.Append(cmdrRequest.Cmdr);
        sb.Append(cmdrRequest.Uploaders);
        return sb.ToString();
    }

    public static string GenMemKey(this CrossTableRequest crossTableRequest)
    {
        var sb = new StringBuilder();
        sb.Append("TeamTable");
        sb.Append(crossTableRequest.TimePeriod);
        sb.Append(crossTableRequest.Mode);
        sb.Append(crossTableRequest.TeMaps);
        return sb.ToString();
    }

    public static string GenMemKey(this RatingChangesRequest ratingChangesRequest)
    {
        var fromDate = RatingRepository.GetRatingChangesFromDate(ratingChangesRequest.TimePeriod);
        var sb = new StringBuilder();
        sb.Append("ratingChange");
        sb.Append(fromDate.ToString("yyyyMMdd"));
        sb.Append(ratingChangesRequest.RatingType.ToString());
        sb.Append(ratingChangesRequest.TimePeriod.ToString());
        return sb.ToString();
    }

    public static string GenMemKey(this CmdrStrengthRequest request)
    {
        StringBuilder sb = new();
        sb.Append("cmdrStrength");
        sb.Append(request.RatingType.ToString());
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.Interest);
        sb.Append('|');
        sb.Append(request.Team);
        return sb.ToString();
    }

    public static string GenMemKey(this DistributionRequest request)
    {
        StringBuilder sb = new();
        sb.Append("dist");
        sb.Append(request.RatingType.ToString());
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.ToonId);
        sb.Append(request.Interest);
        return sb.ToString();
    }

    public static string GenMemKey(this BuildRatingRequest request)
    {
        StringBuilder sb = new();
        sb.Append("buildrating");
        sb.Append(request.RatingType.ToString());
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.Interest);
        sb.Append(request.Vs);
        sb.Append($"{request.FromRating}-{request.ToRating}");
        sb.Append(request.Breakpoint.ToString());
        return sb.ToString();
    }

    public static string GenMemKey(this DurationRequest request)
    {
        StringBuilder sb = new();
        sb.Append("StatsDuration");
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.WithBrawl);
        sb.Append(request.WithRating);
        return sb.ToString();
    }

    public static string GenMemKey(this TimelineRequest request)
    {
        StringBuilder sb = new();
        sb.Append("StatsTimeline");
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.RatingType.ToString());
        return sb.ToString();
    }

    public static string GenMemKey(this WinrateRequest request)
    {
        StringBuilder sb = new();
        sb.Append("StatsWinrate");
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.RatingType.ToString());
        sb.Append(request.FromRating.ToString());
        sb.Append(request.Interest.ToString());
        sb.Append(request.ToRating.ToString());
        return sb.ToString();
    }

    public static string GenMemKey(this SynergyRequest request)
    {
        StringBuilder sb = new();
        sb.Append("StatsSynergy");
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.FromRating);
        sb.Append(request.RatingType.ToString());
        sb.Append(request.ToRating);
        sb.Append(request.WithLeavers.ToString());
        sb.Append(request.Exp2WinOffset.ToString());
        return sb.ToString();
    }

    public static string GenMemKey(this DamageRequest request)
    {
        StringBuilder sb = new();
        sb.Append("StatsDamage");
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.Exp2WinOffset.ToString());
        sb.Append(request.RatingType.ToString());
        sb.Append(request.FromRating.ToString());
        sb.Append(request.Interest.ToString());
        sb.Append(request.ToRating.ToString());
        return sb.ToString();
    }


    public static string GetMd5Hash(MD5 md5Hash, string input)
    {
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder sBuilder = new();
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        return sBuilder.ToString();
    }

    public static string GetHash(this ReplayDsRDto replay)
    {
        StringBuilder sb = new();

        sb.Append((int)replay.GameMode);
        sb.Append(replay.Playercount);
        sb.Append(replay.WinnerTeam);
        sb.Append(replay.TournamentEdition);
        sb.Append(String.Concat(replay.ReplayPlayers.OrderBy(o => o.GamePos).Select(s => $"{s.GamePos}{s.Player.ToonId}")));
        return sb.ToString();
    }

    public static void GenHash(this ArcadeReplay replay, MD5 md5)
    {
        StringBuilder sb = new();
        sb.Append((int)replay.GameMode);
        sb.Append(replay.PlayerCount);
        sb.Append(replay.RegionId);
        sb.Append(replay.TournamentEdition);
        sb.Append(String.Concat(replay.ArcadeReplayPlayers.OrderBy(o => o.ArcadePlayer.ProfileId).Select(s => $"{s.ArcadePlayer.ProfileId}|")));
        replay.ReplayHash = GetMd5Hash(md5, sb.ToString());
    }

    public static string GenArcadeHash(this ReplayDsRDto replay, MD5 md5)
    {        
        StringBuilder sb = new();
        sb.Append((int)replay.GameMode);
        sb.Append(replay.Playercount);
        sb.Append(replay.ReplayPlayers.FirstOrDefault()?.Player.RegionId ?? 0);
        sb.Append(replay.TournamentEdition);
        sb.Append(String.Concat(replay.ReplayPlayers.OrderBy(o => o.Player.ToonId).Select(s => $"{s.Player.ToonId}|")));
        return GetMd5Hash(md5, sb.ToString());
    }
}