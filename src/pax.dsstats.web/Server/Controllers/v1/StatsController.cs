using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.Text.Json.Serialization;

namespace pax.dsstats.web.Server.Controllers.v1
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IReplayRepository replayRepository;
        private readonly BuildService buildService;
        private readonly IStatsService statsService;
        private readonly CmdrsService cmdrService;

        public StatsController(IReplayRepository replayRepository,
                               BuildService buildService,
                               IStatsService statsService,
                               CmdrsService cmdrService)
        {
            this.replayRepository = replayRepository;
            this.buildService = buildService;
            this.statsService = statsService;
            this.cmdrService = cmdrService;
        }

        [HttpGet]
        [Route("GetReplay/{replayHash}")]
        public async Task<ActionResult<ReplayDto?>> GetReplay(string replayHash, CancellationToken token = default)
        {
            var replayDto = await replayRepository.GetReplay(replayHash, false, token);
            if (replayDto == null)
            {
                return NotFound();
            }
            return replayDto;
        }

        [HttpPost]
        [Route("GetReplaysCount")]
        public async Task<ActionResult<int>> GetReplaysCount(ReplaysRequestV1 request, CancellationToken token = default)
        {
            try
            {
                return await replayRepository.GetReplaysCount(request.GetReplaysRequest(), token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetReplays")]
        public async Task<ActionResult<ICollection<ReplayListDto>>> GetReplays(ReplaysRequestV1 request, CancellationToken token = default)
        {
            try
            {
                return Ok(await replayRepository.GetReplays(request.GetReplaysRequest(), token));
            }
            catch (OperationCanceledException)
            {
                return NoContent();
            }
        }

        [HttpPost]
        [Route("GetEventReplaysCount")]
        public async Task<ActionResult<int>> GetEventReplaysCount(ReplaysRequestV1 request, CancellationToken token = default)
        {
            try
            {
                return await replayRepository.GetEventReplaysCount(request.GetReplaysRequest(), token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetEventReplays")]
        public async Task<ActionResult<ICollection<ReplayListEventDto>>> GetEventReplays(ReplaysRequestV1 request, CancellationToken token = default)
        {
            try
            {
                return Ok(await replayRepository.GetEventReplays(request.GetReplaysRequest(), token));
            }
            catch (OperationCanceledException)
            {
                return NoContent();
            }
        }

        [HttpGet]
        [Route("GetTournaments")]
        public async Task<List<EventListDto>> GetTournaments()
        {
            return await replayRepository.GetTournaments();
        }

        [HttpPost]
        [Route("GetStats")]
        public async Task<StatsResponseV1> GetStats(StatsRequestV1 request, CancellationToken token = default)
        {
            var response = await statsService.GetStatsResponse(request.GetStatsRequest());
            return new(response, request);
        }

        [HttpPost]
        [Route("GetTourneyStats")]
        public async Task<StatsResponseV1> GetTourneyStats(StatsRequestV1 request, CancellationToken token = default)
        {
            var response = await statsService.GetTourneyStats(request.GetStatsRequest(), token);
            return new(response, request);
        }

        [HttpPost]
        [Route("GetBuild")]
        public async Task<BuildResponse> GetBuild(BuildRequestV1 request)
        {
            return await buildService.GetBuild(request.GetBuildRequest());
        }

        [HttpGet]
        [Route("GetPlayerDetails/{toonId}")]
        public async Task<PlayerDetailDto> GetPlayerDetails(int toonId)
        {
            return await statsService.GetPlayerDetails(toonId);
        }

        [HttpGet]
        [Route("GetPlayerDetailsNg/{toonId}/{rating}")]
        public async Task<ActionResult<PlayerDetailsResultV5>> GetPlayerDetailsNg(int toonId, int rating, CancellationToken token)
        {
            try
            {
                var result = await statsService.GetPlayerDetails(toonId, (RatingType)rating, token);
                return Ok(new PlayerDetailsResultV5(result));
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpGet]
        [Route("GetPlayerGroupDetails/{toonId}/{rating}")]
        public async Task<ActionResult<PlayerDetailsGroupResult>> GetPlayerGroupDetails(int toonId, int rating, CancellationToken token)
        {
            try
            {
                return await statsService.GetPlayerGroupDetails(toonId, (RatingType)rating, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpGet]
        [Route("GetPlayerMatchups/{toonId}/{rating}")]
        public async Task<ActionResult<List<PlayerMatchupInfo>>> GetPlayerMatchups(int toonId, int rating, CancellationToken token)
        {
            try
            {
                return await statsService.GetPlayerMatchups(toonId, (RatingType)rating, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetCmdrInfo")]
        public async Task<ActionResult<CmdrResult>> GetCmdrInfo(CmdrRequestV1 cmdrRequest, CancellationToken token = default)
        {
            try
            {
                return await cmdrService.GetCmdrInfo(cmdrRequest.GetCmdrRequest(), token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetCrosstable")]
        public async Task<ActionResult<CrossTableResponse>> GetCrosstable(CrossTableRequestV1 request, CancellationToken token = default)
        {
            try
            {
                return await statsService.GetCrossTable(request.GetCrossTableRequest(), token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetTeamReplays")]
        public async Task<ActionResult<List<BuildResponseReplay>>> GetTeamReplays(CrossTableReplaysRequestV1 request, CancellationToken token)
        {
            try
            {
                return await statsService.GetTeamReplays(request.GetCrossTableReplaysRequest(), token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetStatsUpgrades")]
        public async Task<ActionResult<StatsUpgradesResponse>> GetUpgradeStats(BuildRequestV1 buildRequest, CancellationToken token)
        {
            try
            {
                return await statsService.GetUpgradeStats(buildRequest.GetBuildRequest(), token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetGameInfo")]
        public async Task<ActionResult<GameInfoResult>> GetGameInfo(GameInfoRequest request, CancellationToken token)
        {
            try
            {
                return await statsService.GetGameInfo(request, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }
    }
}

public record StatsResponseV1
{
    public StatsRequestV1 Request { get; init; } = new();
    public ICollection<StatsResponseItem> Items { get; init; } = new List<StatsResponseItem>();
    public CountResponse CountResponse { get; init; } = new();
    public int Count { get; init; }
    public int Bans { get; set; }
    public int AvgDuration { get; init; }

    public StatsResponseV1(StatsResponse statsResponse, StatsRequestV1 statsRequestV1)
    {
        Request = statsRequestV1;
        Items = statsResponse.Items;
        CountResponse = statsResponse.CountResponse;
        Count = statsResponse.Count;
        Bans = statsResponse.Bans;
        AvgDuration = statsResponse.AvgDuration;
    }
}

public record ReplaysRequestV1
{
    public List<TableOrder> Orders { get; set; } = new List<TableOrder>() { new TableOrder() { Property = "GameTime" } };
    public DateTime StartTime { get; set; } = new DateTime(2022, 2, 1);
    public DateTime EndTime { get; set; } = DateTime.Today;
    public int Skip { get; set; }
    public int Take { get; set; }
    public string? Tournament { get; set; }
    public string? SearchString { get; set; }
    public string? SearchPlayers { get; set; }
    public bool LinkSearch { get; set; }
    public bool ResultAdjusted { get; set; }
    public string? ReplayHash { get; set; }
    public bool DefaultFilter { get; set; }
    public int PlayerCount { get; set; }
    public List<GameMode> GameModes { get; set; } = new();
    public bool WithMmrChange { get; set; }
    public int ToonId { get; set; }
    public int ToonIdWith { get; set; }
    public int ToonIdVs { get; set; }
    public string? ToonIdName { get; set; }

    public ReplaysRequest GetReplaysRequest()
    {
        return new()
        {
            Orders = Orders,
            Skip = Skip,
            Take = Take,
            Tournament = Tournament,
            SearchString = SearchString,
            SearchPlayers = SearchPlayers,
            LinkSearch = LinkSearch,
            ResultAdjusted = ResultAdjusted,
            ReplayHash = ReplayHash,
            DefaultFilter = DefaultFilter,
            PlayerCount = PlayerCount,
            GameModes = GameModes,
            WithMmrChange = WithMmrChange,
            ToonId = ToonId,
            ToonIdWith = ToonIdWith,
            ToonIdVs = ToonIdVs,
            ToonIdName = ToonIdName,
        };
    }
}

public record CmdrRequestV1
{
    public Commander Cmdr { get; set; }
    public string TimeSpan { get; set; } = "This Year";
    public bool Uploaders { get; set; }

    public CmdrRequest GetCmdrRequest()
    {
        return new()
        {
            Cmdr = Cmdr,
            TimeSpan = Data.GetTimePeriodFromDeprecatedString(TimeSpan),
            Uploaders = Uploaders
        };
    }
}

public record CrossTableRequestV1
{
    public string Mode { get; set; } = "Standard";
    public string TimePeriod { get; set; } = "Last Two Years";
    public bool TeMaps { get; set; }

    public CrossTableRequest GetCrossTableRequest()
    {
        return new()
        {
            Mode = Mode,
            TimePeriod = Data.GetTimePeriodFromDeprecatedString(TimePeriod),
            TeMaps = TeMaps
        };
    }
}

public record CrossTableReplaysRequestV1
{
    public string Mode { get; set; } = "Standard";
    public string TimePeriod { get; set; } = "Last Two Years";
    public bool TeMaps { get; set; }
    public TeamCmdrs TeamCmdrs { get; set; } = null!;
    public TeamCmdrs? TeamCmdrsVs { get; set; }

    public CrossTableReplaysRequest GetCrossTableReplaysRequest()
    {
        return new()
        {
            Mode = Mode,
            TimePeriod = Data.GetTimePeriodFromDeprecatedString(TimePeriod),
            TeMaps = TeMaps,
            TeamCmdrs = TeamCmdrs,
            TeamCmdrsVs = TeamCmdrsVs
        };
    }
}

public record StatsRequestV1
{
    public StatsMode StatsMode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string TimePeriod { get; set; } = "This Year";
    [JsonIgnore]
    public bool BeginAtZero { get; set; }
    [JsonIgnore]
    public List<Commander> AddRemoveCommanders { get; set; } = new();
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public bool Uploaders { get; set; }
    public bool DefaultFilter { get; set; } = true;
    public bool TeMaps { get; set; }
    public int PlayerCount { get; set; }
    public List<RequestNames> PlayerNames { get; set; } = new();
    public List<GameMode> GameModes { get; set; } = new();
    public string? Tournament { get; set; }
    public string? Round { get; set; }

    public StatsRequest GetStatsRequest()
    {
        return new()
        {
            StatsMode = StatsMode,
            TimePeriod = Data.GetTimePeriodFromDeprecatedString(TimePeriod),
            Interest = Interest,
            Versus = Versus,
            Uploaders = Uploaders,
            DefaultFilter = DefaultFilter,
            TeMaps = TeMaps,
            PlayerCount = PlayerCount,
            PlayerNames = PlayerNames,
            GameModes = GameModes,
            Tournament = Tournament,
            Round = Round
        };
    }
}