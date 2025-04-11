
using System.Security.Cryptography;
using dsstats.db.Services.Ratings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db.Tests;

[TestClass]
public class RatingsTests
{
    private readonly ServiceProvider serviceProvider;

    public RatingsTests()
    {
        var services = TestServiceCollection.GetServiceCollection();
        services.AddSingleton<dsstats.db.Services.Import.ImportService>();
        services.AddSingleton<RatingsService>();
        serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task T01BasicRatingsTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<dsstats.db.Services.Import.ImportService>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        using var md5 = MD5.Create();
        var replayDto = ImportTests.GetBasicReplayDto(md5);

        await importService.Import([replayDto]);
        await ratingsService.CalculateRatings();

        var replayRatings = context.ReplayRatings.Count();
        Assert.IsTrue(replayRatings > 0);
        var playerRatings = context.PlayerRatings.Count();
        Assert.IsTrue(playerRatings > 0);
        var replayPlayerRatings = context.ReplayPlayerRatings.Count();
        Assert.IsTrue(replayPlayerRatings > 0);
    }

    [TestMethod]
    public async Task T02ArcadeMatchTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<dsstats.db.Services.Import.ImportService>();

        using var md5 = MD5.Create();
        var replayDto = ImportTests.GetBasicReplayDto(md5);

        await importService.Import([replayDto]);

        var arcadeReplayDto = ImportTests.GetArcadeReplayDto(replayDto);

        await importService.ImportArcadeReplays([arcadeReplayDto]);

        await importService.CombineDsstatsSc2ArcadeReplays();

        var matches = context.ReplayArcadeMatches.Count();
        Assert.IsTrue(matches > 0);
    }

    [TestMethod]
    public async Task T03ArcadeRatingsTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<dsstats.db.Services.Import.ImportService>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        using var md5 = MD5.Create();
        var replayDto = ImportTests.GetBasicReplayDto(md5);
        await importService.Import([replayDto]);

        var arcadeReplayDto = ImportTests.GetArcadeReplayDto(replayDto);
        var arcadeReplayDto2 = ImportTests.GetArcadeReplayDto();

        arcadeReplayDto2.ArcadeReplayDsPlayers.Remove(arcadeReplayDto2.ArcadeReplayDsPlayers.First(f => f.SlotNumber == 1));
        arcadeReplayDto2.ArcadeReplayDsPlayers.Add(arcadeReplayDto.ArcadeReplayDsPlayers.First(f => f.SlotNumber == 1));

        await importService.ImportArcadeReplays([arcadeReplayDto, arcadeReplayDto2]);

        await importService.CombineDsstatsSc2ArcadeReplays();

        await ratingsService.CalculateRatings();

        var playerRatings = context.PlayerRatings.ToList();
        Assert.IsTrue(playerRatings.Count(x => x.ArcadeGames > 0) > 0);
    }

    [TestMethod]
    public async Task T04ContinueRatingsTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<dsstats.db.Services.Import.ImportService>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        using var md5 = MD5.Create();
        var replayDto = ImportTests.GetBasicReplayDto(md5);
        await importService.Import([replayDto]);

        await ratingsService.ContinueCalculateRatings();

        var playerRatings = context.PlayerRatings.Count();
        Assert.IsTrue(playerRatings > 0);
    }
}