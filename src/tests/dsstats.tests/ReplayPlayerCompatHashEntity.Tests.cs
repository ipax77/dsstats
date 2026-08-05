using dsstats.db;
using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public sealed class ReplayPlayerCompatHashEntityTests
{
    [TestMethod]
    public void CanRoundTripReplayPlayerCompatHash()
    {
        ReplayDto replayDto = new()
        {
            GameMode = GameMode.Commanders,
            Players =
            [
                new()
                {
                    CompatHash = "ds-player-compat-v1-test",
                    Name = "PAX",
                    Race = Commander.Raynor,
                    SelectedRace = Commander.Terran,
                    GamePos = 1,
                    TeamId = 1,
                    Player = new()
                    {
                        Name = "PAX",
                        ToonId = new() { Region = 1, Realm = 1, Id = 1 },
                    },
                },
            ],
        };

        var entity = replayDto.Players[0].ToEntity(replayDto);
        var dto = entity.ToDto();

        Assert.AreEqual("ds-player-compat-v1-test", dto.CompatHash);
    }
}
