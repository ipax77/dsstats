using System.Net.Http.Headers;
using System.Text.Json;

using AutoMapper;
using dsstats.mmr;
using dsstats.mmr.ProcessData;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using SqliteMigrations.Migrations;

namespace dsstats.maui.tests;

public class LeaverHandlingTests : TestWithSqlite
{
    readonly (ReplayDsRDto, ReplayDto, Dictionary<int, CalcRating>) baseReplay;
    readonly MmrOptions mmrOptions;

    public LeaverHandlingTests()
    {
        mmrOptions = new(true);
        baseReplay = GetBaseReplay();
    }

    private (ReplayDsRDto, ReplayDto, Dictionary<int, CalcRating>) GetBaseReplay()
    {
        var replayDto = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText("/data/testdata/testreplayDto.json"));
        if (replayDto == null)
        {
            Assert.Fail("ERROR: replayDto == null");
        }

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new AutoMapperProfile());
        });
        var mapper = mapperConfiguration.CreateMapper();

        var mmrIdRatings = new Dictionary<int, CalcRating>();
        var replayDsRDto = mapper.Map<ReplayDsRDto>(replayDto);

        for (int i = 0; i < replayDsRDto.ReplayPlayers.Count; i++)
        {
            replayDsRDto.ReplayPlayers[i] = replayDsRDto.ReplayPlayers[i] with
            {
                Player = new PlayerDsRDto() with
                {
                    PlayerId = i
                }
            };

            mmrIdRatings.Add(i, new CalcRating()
            {
                PlayerId = i,
                Mmr = mmrOptions.StartMmr,
                Consistency = 0,
                Confidence = 0,
                Games = 0,
            });
        }

        return (replayDsRDto with { }, replayDto with { }, mmrIdRatings);
    }

    [Fact]
    public void NoneLeaver()
    {
        var (mockReplay, replayDto, mmrIdRatings) = baseReplay with { };

        mockReplay = mockReplay with { Maxleaver = 0 };
        for (int i = 0; i < mockReplay.ReplayPlayers.Count; i++)
        {
            mockReplay.ReplayPlayers[i] = mockReplay.ReplayPlayers[i] with { Duration = replayDto.Duration };
        }

        var plChanges = MmrService.ProcessReplay(new ReplayData(mockReplay), mmrIdRatings, new(), mmrOptions);

        var winnerPlayers = replayDto.ReplayPlayers.Where(x => x.PlayerResult == PlayerResult.Win).ToArray();
        var loserPlayers = replayDto.ReplayPlayers.Where(x => x.PlayerResult == PlayerResult.Los).ToArray();

        var winnersChange = winnerPlayers.Sum(p => plChanges.Find(x => x.Pos == p.GamePos)?.Change) / winnerPlayers.Length;
        var loserChange = loserPlayers.Sum(p => plChanges.Find(x => x.Pos == p.GamePos)?.Change) / loserPlayers.Length;

        foreach (var player in winnerPlayers)
        {
            var plChange = plChanges.FirstOrDefault(f => f.Pos == player.GamePos);
            Assert.True(plChange?.Change == winnersChange);
        }

        foreach (var player in loserPlayers)
        {
            var plChange = plChanges.FirstOrDefault(f => f.Pos == player.GamePos);
            Assert.True(plChange?.Change == loserChange);
        }
    }

    [Fact]
    public void OneLeaver()
    {
        var (mockReplay, replayDto, mmrIdRatings) = baseReplay with { };

        mockReplay = mockReplay with { Maxleaver = 91 };
        for (int i = 0; i < mockReplay.ReplayPlayers.Count; i++)
        {
            if (i == 0)
            {
                mockReplay.ReplayPlayers[i] = mockReplay.ReplayPlayers[i] with { Duration = 0 };
            }
            else
            {
                mockReplay.ReplayPlayers[i] = mockReplay.ReplayPlayers[i] with { Duration = replayDto.Duration };
            }
        }

        var plChanges = MmrService.ProcessReplay(new ReplayData(mockReplay), mmrIdRatings, new(), new(true));

        var winnerPlayers = replayDto.ReplayPlayers.Where(x => x.PlayerResult == PlayerResult.Win).ToArray();
        var loserPlayers = replayDto.ReplayPlayers.Where(x => x.PlayerResult == PlayerResult.Los).ToArray();

        var leaverChange = plChanges.Min(x => x.Change);
        //var winnersChange = winnerPlayers.Sum(p => plChanges.Find(x => x.Pos == p.GamePos)?.Change) / winnerPlayers.Length;
        //var loserChange = loserPlayers.Sum(p => plChanges.Find(x => x.Pos == p.GamePos)?.Change) / loserPlayers.Length;
        
        foreach (var player in winnerPlayers)
        {
            var plChange = plChanges.FirstOrDefault(f => f.Pos == player.GamePos);

            if (player == replayDto.ReplayPlayers.ElementAt(0))
            {
                Assert.True(plChange?.Change == leaverChange);
            }
            else
            {
                Assert.True(plChange?.Change == -0.5 * leaverChange);
            }
        }

        foreach (var player in loserPlayers)
        {
            var plChange = plChanges.FirstOrDefault(f => f.Pos == player.GamePos);
            Assert.True(plChange?.Change == 0.5 * leaverChange);
        }
    }
}