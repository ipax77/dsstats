using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace dsstats.services;

public class DsstatsService : IDsstatsService
{
    private readonly ReplayContext context;
    private readonly ILogger<DsstatsService> logger;
    private readonly int pageSize = 50;

    public DsstatsService(ReplayContext context, ILogger<DsstatsService> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task<DsstatsReplaysResponse> GetReplays(string? page)
    {
        int skip = Base64Decode(page);

        var replays = await context.Replays
            .OrderByDescending(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Where(x => x.GameTime > new DateTime(2018, 1, 1))
            .Skip(skip)
            .Take(pageSize)
            .Select(s => new ReplayListDto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = (GameMode)s.GameMode,
                ReplayHash = s.ReplayHash,
                DefaultFilter = s.DefaultFilter,
                CommandersTeam1 = s.CommandersTeam1,
                CommandersTeam2 = s.CommandersTeam2,
                MaxLeaver = s.Maxleaver
            })
            .ToListAsync();

        return new()
        {
            Page = GetPage(page, skip, replays.Count),
            Replays = replays,
        };
    }

    private DsstatsPage GetPage(string? currentPage, int skip, int count)
    {
        return new()
        {
            Prev = currentPage,
            Next = count < pageSize ? null : Base64Encode(skip + pageSize),
        };
    }

    private static string Base64Encode(int skip)
    {
        var bytes = Encoding.UTF8.GetBytes(skip.ToString(CultureInfo.InvariantCulture));
        return Convert.ToBase64String(bytes);
    }

    private int Base64Decode(string? page)
    {
        if (string.IsNullOrEmpty(page))
        {
            return 0;
        }

        try
        {
            var base64EncodedBytes = Convert.FromBase64String(page);
            var encPage = Encoding.UTF8.GetString(base64EncodedBytes);
            if (int.TryParse(encPage, out var enc))
            {
                return enc;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Failed decoding page: {error}", ex.Message);
        }
        return 0;
    }
}



public record DsstatsReplaysResponse
{
    public DsstatsPage Page { get; set; } = new();
    public List<ReplayListDto> Replays { get; set; } = new();
}

public record DsstatsPage
{
    public string? Prev { get; set; }
    public string? Next { get; set; }
}
