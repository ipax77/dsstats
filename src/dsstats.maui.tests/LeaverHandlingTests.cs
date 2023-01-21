using System.Text.Json;

using AutoMapper;
using dsstats.mmr;
using pax.dsstats.dbng;
using pax.dsstats.shared;

namespace dsstats.maui.tests;

public class LeaverHandlingTests : TestWithSqlite
{
    private readonly IMapper mapper;

    public LeaverHandlingTests()
    {
        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new AutoMapperProfile());
        });
        mapper = mapperConfiguration.CreateMapper();
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    private (ReplayDsRDto, ReplayDto, Dictionary<int, CalcRating>) GetBaseReplay(string filePath)
    {
        var replayDto = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(filePath));
        if (replayDto == null)
        {
            Assert.Fail("ERROR: replayDto == null");
        }
        replayDto = replayDto with
        {
            ReplayPlayers = replayDto.ReplayPlayers
                .OrderBy(x => x.Team)
                    .ThenBy(x => x.GamePos)
                .ToArray()
        };

        var mmrOptions = new MmrOptions(true);
        var mmrIdRatings = new Dictionary<int, CalcRating>();

        var replay = mapper.Map<Replay>(replayDto);
        var replayDsRDto = mapper.Map<ReplayDsRDto>(replay);

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
                Mmr = mmrOptions.StartMmr + (i * 200),
                Consistency = 0,
                Confidence = 0,
                Games = 0,
            });
        }

        return (replayDsRDto with { }, replayDto with { }, mmrIdRatings);
    }
    private static bool IsLeaver(ReplayDsRDto replay, ReplayPlayerDsRDto replayPlayer)
    {
        return replayPlayer.Duration < replay.Duration - 90 || (replayPlayer.IsUploader && replay.ResultCorrected);
    }

    [Theory]
    [InlineData("/data/testdata/team1Win.json")]
    [InlineData("/data/testdata/team2Win.json")]
    public void NoneLeaver(string filePath)
    {
        var (mockReplay, replayDto, mmrIdRatings) = GetBaseReplay(filePath) with { };

        mockReplay = mockReplay with { Maxleaver = 0 };
        for (int i = 0; i < mockReplay.ReplayPlayers.Count; i++)
        {
            mockReplay.ReplayPlayers[i] = mockReplay.ReplayPlayers[i] with { Duration = replayDto.Duration };
        }

        var replayRatingDto = MmrService.ProcessReplay(mockReplay, mmrIdRatings, new(), new(true))!;

        var winnerPlayers = replayDto.ReplayPlayers.Where(x => x.PlayerResult == PlayerResult.Win).ToArray();
        var loserPlayers = replayDto.ReplayPlayers.Where(x => x.PlayerResult == PlayerResult.Los).ToArray();

        var winnersChange = winnerPlayers.Sum(p => replayRatingDto.RepPlayerRatings.Find(x => x.GamePos == p.GamePos)?.RatingChange) / winnerPlayers.Length;
        var loserChange = loserPlayers.Sum(p => replayRatingDto.RepPlayerRatings.Find(x => x.GamePos == p.GamePos)?.RatingChange) / loserPlayers.Length;

        foreach (var player in winnerPlayers)
        {
            var plChange = replayRatingDto.RepPlayerRatings.FirstOrDefault(f => f.GamePos == player.GamePos);
            Assert.Equal(plChange?.RatingChange, winnersChange);
        }

        foreach (var player in loserPlayers)
        {
            var plChange = replayRatingDto.RepPlayerRatings.FirstOrDefault(f => f.GamePos == player.GamePos);
            Assert.Equal(plChange?.RatingChange, loserChange);
        }
    }

    [Theory]
    [InlineData("/data/testdata/team1Win.json")]
    [InlineData("/data/testdata/team2Win.json")]
    public void OneLeaver(string filePath)
    {
        var (mockReplay, replayDto, mmrIdRatings) = GetBaseReplay(filePath) with { };

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

        var replayRatingDto = MmrService.ProcessReplay(mockReplay, mmrIdRatings, new(), new(true))!;

        var leaverPlayers = mockReplay.ReplayPlayers.Where(x => IsLeaver(mockReplay, x)).ToArray();
        var winnerPlayers = mockReplay.ReplayPlayers.Where(x => (x.PlayerResult == PlayerResult.Win) && !IsLeaver(mockReplay, x)).ToArray();
        var loserPlayers = mockReplay.ReplayPlayers.Where(x => (x.PlayerResult == PlayerResult.Los) && !IsLeaver(mockReplay, x)).ToArray();

        var leaverChange = leaverPlayers.Sum(p => replayRatingDto.RepPlayerRatings.Find(x => x.GamePos == p.GamePos)?.RatingChange) / leaverPlayers.Length;

        //var winnersChange = winnerPlayers.Sum(p => plChanges.Find(x => x.Pos == p.GamePos)?.Change) / winnerPlayers.Length;
        //var loserChange = loserPlayers.Sum(p => plChanges.Find(x => x.Pos == p.GamePos)?.Change) / loserPlayers.Length;

        if (winnerPlayers.Length < loserPlayers.Length) // Leavers are in winnerTeam
        {
            return; // No solution to test properly yet!!!
        }

        foreach (var player in winnerPlayers)
        {
            var repPlayerRatingDto = replayRatingDto.RepPlayerRatings.FirstOrDefault(f => f.GamePos == player.GamePos);

            if (player == mockReplay.ReplayPlayers.ElementAt(0))
            {
                Assert.Equal(repPlayerRatingDto?.RatingChange, leaverChange);
            }
            else
            {
                Assert.Equal(repPlayerRatingDto?.RatingChange, -0.5 * leaverChange);
            }
        }

        foreach (var player in loserPlayers)
        {
            var repPlayerRatingDto = replayRatingDto.RepPlayerRatings.FirstOrDefault(f => f.GamePos == player.GamePos);
            Assert.Equal(repPlayerRatingDto?.RatingChange, 0.5 * leaverChange);
        }
    }

    [Theory]
    [InlineData("/data/testdata/team1Win.json")]
    [InlineData("/data/testdata/team2Win.json")]
    public void TwoLeaversSameTeam(string filePath)
    {
        var (mockReplay, replayDto, mmrIdRatings) = GetBaseReplay(filePath) with { };

        mockReplay = mockReplay with { Maxleaver = 91 };
        for (int i = 0; i < mockReplay.ReplayPlayers.Count; i++)
        {
            if (i == 0 || i == 1)
            {
                mockReplay.ReplayPlayers[i] = mockReplay.ReplayPlayers[i] with { Duration = 0 };
            }
            else
            {
                mockReplay.ReplayPlayers[i] = mockReplay.ReplayPlayers[i] with { Duration = replayDto.Duration };
            }
        }

        var replayRatingDto = MmrService.ProcessReplay(mockReplay, mmrIdRatings, new(), new(true))!;

        var leaverPlayers = mockReplay.ReplayPlayers.Where(x => IsLeaver(mockReplay, x)).ToArray();
        var winnerPlayers = mockReplay.ReplayPlayers.Where(x => (x.PlayerResult == PlayerResult.Win) && !IsLeaver(mockReplay, x)).ToArray();
        var loserPlayers = mockReplay.ReplayPlayers.Where(x => (x.PlayerResult == PlayerResult.Los) && !IsLeaver(mockReplay, x)).ToArray();

        var leaverChange = leaverPlayers.Sum(p => replayRatingDto.RepPlayerRatings.Find(x => x.GamePos == p.GamePos)?.RatingChange) / leaverPlayers.Length;

        //var winnersChange = winnerPlayers.Sum(p => replayRatingDto.RepPlayerRatings.Find(x => x.GamePos == p.GamePos)?.RatingChange) / winnerPlayers.Length;
        //var loserChange = loserPlayers.Sum(p => replayRatingDto.RepPlayerRatings.Find(x => x.GamePos == p.GamePos)?.RatingChange) / loserPlayers.Length;

        if (winnerPlayers.Length < loserPlayers.Length) // Leavers are in winnerTeam
        {
            return; // No solution to test properly yet!!!
        }

        foreach (var player in winnerPlayers)
        {
            var repPlayerRatingDto = replayRatingDto.RepPlayerRatings.FirstOrDefault(f => f.GamePos == player.GamePos);

            if (player == mockReplay.ReplayPlayers.ElementAt(0) || player == mockReplay.ReplayPlayers.ElementAt(1))
            {
                Assert.Equal(repPlayerRatingDto?.RatingChange, leaverChange);
            }
            else
            {
                Assert.Equal(repPlayerRatingDto?.RatingChange, -0.25 * leaverChange);
            }
        }

        foreach (var player in loserPlayers)
        {
            var repPlayerRatingDto = replayRatingDto.RepPlayerRatings.FirstOrDefault(f => f.GamePos == player.GamePos);

            if (player == mockReplay.ReplayPlayers.ElementAt(0) || player == mockReplay.ReplayPlayers.ElementAt(1))
            {
                Assert.Equal(repPlayerRatingDto?.RatingChange, leaverChange);
            }
            else
            {
                Assert.Equal(repPlayerRatingDto?.RatingChange, 0.25 * leaverChange);
            }
        }
    }
}