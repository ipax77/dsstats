
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class CheatDetectService
{
    private readonly ReplayContext context;
    private readonly IMapper mapper;
    private readonly ILogger<CheatDetectService> logger;

    public CheatDetectService(ReplayContext context,
                              IMapper mapper,
                              ILogger<CheatDetectService> logger)
    {
        this.context = context;
        this.mapper = mapper;
        this.logger = logger;
    }

    private static int GetMiddleScore(int middle)
    {
        if (middle > 0)
        {
            return -2;
        }
        if (middle < -800)
        {
            return 3;
        }
        if (middle < -400)
        {
            return 2;
        }
        if (middle < 0)
        {
            return 1;
        }
        return 0;
    }

    private static int GetNumberScore(int number)
    {
        if (number < 0)
        {
            return 1;
        }
        if (number > 0)
        {
            return -1;
        }
        return 0;
    }

    private static int GetUploaderLastMiddleHold(string middle, int duration, int uploaderTeam)
    {
        if (String.IsNullOrEmpty(middle) || duration == 0)
        {
            return 0;
        }

        int totalDuration = (int)(duration * 22.4);
        var ents = middle.Split('|', StringSplitOptions.RemoveEmptyEntries);

        if (ents.Length < 2)
        {
            return 0;
        }

        int currentTeam = int.Parse(ents[0]);
        int lastLoop = int.Parse(ents[1]);

        int sumTeam1 = 0;
        int sumTeam2 = 0;

        if (ents.Length > 2)
        {
            for (int i = 2; i < ents.Length; i++)
            {
                int currentLoop = int.Parse(ents[i]);
                if (currentTeam == 1)
                {
                    sumTeam1 += currentLoop - lastLoop;
                }
                else
                {
                    sumTeam2 += currentLoop - lastLoop;
                }
                currentTeam = currentTeam == 1 ? 2 : 1;
                lastLoop = currentLoop;
            }
        }

        int lastHoldDuration = totalDuration - sumTeam1 - sumTeam2;

        // sumTeam1 = currentTeam == 1 ? sumTeam1 + lastHoldDuration : sumTeam1;
        // sumTeam2 = currentTeam == 2 ? sumTeam2 + lastHoldDuration : sumTeam2;

        // var mid1 = sumTeam1 * 100.0 / totalDuration;
        // var mid2 = sumTeam2 * 100.0 / totalDuration;

        return uploaderTeam == currentTeam ? lastHoldDuration : lastHoldDuration * -1;
    }
}

public record CheatResult
{
    public int NoResultGames { get; init; }
    public int RqGames { get; set; }
    public int DcGames { get; set; }
    public int UnknownGames { get; set; }
}

internal record DetectInfo
{
    public int RqGames { get; set; }
    public int DcGames { get; set; }
}