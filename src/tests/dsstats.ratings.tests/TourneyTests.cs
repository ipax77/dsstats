﻿using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.db8services;
using dsstats.db8services.Tourneys;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;


namespace dsstats.ratings.tests;

[TestClass]
public class TourneyTests
{
    private ServiceProvider serviceProvider;
    private readonly List<RequestNames> playerPool;
    private readonly int poolCount = 100;

    public TourneyTests()
    {
        playerPool = new();
        for (int i = 2; i < poolCount + 2; i++)
        {
            playerPool.Add(new($"Test{i}", i, 1, 1));
        }


        var services = new ServiceCollection();
        var serverVersion = new MySqlServerVersion(new Version(5, 7, 44));
        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("TestConnectionString").GetString();
        var importConnectionString = config.GetProperty("ImportTestConnectionString").GetString() ?? "";

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
            {
                x.ImportConnectionString = importConnectionString;
                x.IsSqlite = false;
            });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(300);
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddLogging();
        services.AddMemoryCache();
        services.AddAutoMapper(typeof(AutoMapperProfile));

        services.AddScoped<TeamsCreateService>();

        services.AddScoped<IReplayRepository, ReplayRepository>();

        serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task T01CreateTourneyTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var tourneyService = scope.ServiceProvider.GetRequiredService<TeamsCreateService>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();


        TourneyCreateDto createDto = new()
        {
            Name = "TestTournament",
            EventStart = DateTime.Today,
            GameMode = GameMode.Standard
        };

        var result = await tourneyService.CreateTournament(createDto);
        Assert.IsFalse(result == Guid.Empty);
    }

    [TestMethod]
    public async Task T02AddPlayersTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var tourneyService = scope.ServiceProvider.GetRequiredService<TeamsCreateService>();

        var tourney = context.Tourneys.FirstOrDefault();
        Assert.IsNotNull(tourney);

        var players = playerPool.Select(s => new Player()
        {
            Name = s.Name,
            ToonId = s.ToonId,
            RealmId = s.RealmId,
            RegionId = s.RegionId,
            ComboPlayerRatings = new List<ComboPlayerRating>()
            {
                new ComboPlayerRating()
                {
                    RatingType = RatingType.Std,
                    Rating = Random.Shared.Next(500, 2500)
                }
            }
        });

        context.Players.AddRange(players);
        context.SaveChanges();

        var result = await tourneyService.AddTournamentPlayers(new()
        {
            TourneyGuid = tourney.TourneyGuid,
            PlayerIds = players.Take(30).Select(s => new PlayerId(s.ToonId, s.RealmId, s.RegionId)).ToList()
        });

        Assert.IsTrue(result);

        var tourneyAfter = context.Tourneys
            .Include(i => i.TourneyPlayers)
            .FirstOrDefault(i => i.TourneyGuid ==  tourney.TourneyGuid);

        Assert.AreEqual(30, tourneyAfter?.TourneyPlayers.Count());
    }

    [TestMethod]
    public async Task T03CreateTeamsTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var tourneyService = scope.ServiceProvider.GetRequiredService<TeamsCreateService>();

        var tourney = context.Tourneys.FirstOrDefault();
        Assert.IsNotNull(tourney);

        var result = await tourneyService.CreateRandomTeams(tourney.TourneyGuid, RatingType.Std);

        Assert.IsTrue(result);

        var teams = await context.TourneyTeams
            .Where(x => x.Tourney!.TourneyGuid == tourney.TourneyGuid)
            .ToListAsync();

        Assert.AreEqual(10,  teams.Count());
    }

    [TestMethod]
    public async Task T04AddTeamsTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var tourneyService = scope.ServiceProvider.GetRequiredService<TeamsCreateService>();

        var tourney = context.Tourneys.FirstOrDefault();
        Assert.IsNotNull(tourney);

        TourneyTeamCreateDto createDto = new()
        {
            TourneyGuid = tourney.TourneyGuid,
            Name = "TestTeam 77",
            Players = playerPool.Skip(30).Take(3).Select(s => new PlayerId(s.ToonId, s.RealmId, s.RegionId)).ToList()
        };

        var result = await tourneyService.AddTourneyTeam(createDto);

        Assert.IsFalse(result == Guid.Empty);
    }

    [TestMethod]
    public async Task T05AddMatchTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var tourneyService = scope.ServiceProvider.GetRequiredService<TeamsCreateService>();

        var tourney = context.Tourneys
            .Include(i => i.TourneyTeams)
            .FirstOrDefault();
        Assert.IsNotNull(tourney);
        Assert.IsTrue(tourney.TourneyTeams.Count() > 2);

        TourneyMatchCreateDto createDto = new()
        {
            TourneyGuid = tourney.TourneyGuid,
            Round = 1,
            TeamAGuid = tourney.TourneyTeams.ElementAt(0).TeamGuid,
            TeamBGuid = tourney.TourneyTeams.ElementAt(1).TeamGuid,
            Ban1 = Commander.Tychus
        };

        var result = await tourneyService.AddTourneyMatch(createDto);

        Assert.IsFalse(result == Guid.Empty);
    }

    [TestMethod]
    public async Task T06ReportMatchResultTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var tourneyService = scope.ServiceProvider.GetRequiredService<TeamsCreateService>();

        var tourney = context.Tourneys
            .Include(i => i.TourneyMatches)
            .FirstOrDefault();
        Assert.IsNotNull(tourney);
        Assert.IsTrue(tourney.TourneyMatches.Count() > 0);

        var tourneyMatch = tourney.TourneyMatches.First();

        TourneyMatchResult result = new()
        {
            TourneyMatchGuid = tourneyMatch.TourneyMatchGuid,
            MatchResult = MatchResult.TeamAWin,
            Ban1 = Commander.Tychus
        };

        var reportResult = await tourneyService.ReportMatchResult(result);

        Assert.IsTrue(reportResult);
    }

    [TestMethod]
    public async Task T06RoundRobinBracketTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var tourneyService = scope.ServiceProvider.GetRequiredService<TeamsCreateService>();

        var tourney = context.Tourneys
            .Include(i => i.TourneyMatches)
            .FirstOrDefault();
        Assert.IsNotNull(tourney);

        //TourneyTeamCreateDto createDto = new()
        //{
        //    TourneyGuid = tourney.TourneyGuid,
        //    Name = "TestTeam 78",
        //    Players = playerPool.Skip(33).Take(3).Select(s => new PlayerId(s.ToonId, s.RealmId, s.RegionId)).ToList()
        //};
        //await tourneyService.AddTourneyTeam(createDto);

        tourney.TourneyMatches.Clear();
        await context.SaveChangesAsync();

        var result = await tourneyService.CreateRoundRobinBracket(tourney.TourneyGuid);

        Assert.IsTrue(result);

        var tourneyMatches = await context.TourneyMatches
            .Where(x => x.TourneyId == tourney.TourneyId && x.Round == 1)
            .ToListAsync();

        Assert.IsTrue(tourneyMatches.Count > 0);

        var byeMatches = tourneyMatches.Where(x => x.MatchResult == MatchResult.TeamABye)
            .ToList();

        Assert.AreEqual(1, byeMatches.Count);
    }

    [TestMethod]
    public async Task T07SwissTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var tourneyService = scope.ServiceProvider.GetRequiredService<TeamsCreateService>();

        var tourney = context.Tourneys
            .Include(i => i.TourneyMatches)
            .FirstOrDefault();
        Assert.IsNotNull(tourney);

        tourney.TourneyMatches.Clear();
        await context.SaveChangesAsync();

        var result = await tourneyService.CreateNewSwissRound(tourney.TourneyGuid);

        Assert.IsTrue(result);

        var tourneyMatches = await context.TourneyMatches
            .Where(x => x.TourneyId == tourney.TourneyId && x.Round == 1)
            .ToListAsync();

        Assert.IsTrue(tourneyMatches.Count > 0);
    }
}