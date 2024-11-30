using dsstats.shared;

namespace dsstats.pickban.tests
{
    [TestClass]
    public class PickBanTests
    {
        [TestMethod]
        public void InitializeTest()
        {
            var pickBanState = new PickBanState(Guid.NewGuid(), PickBanMode.Commanders, GameMode.Commanders, 2, 6);

            Assert.AreEqual(0, pickBanState.Visitors);
            Assert.AreEqual(0, pickBanState.Bans.Count);
            Assert.AreEqual(0, pickBanState.Picks.Count);
            Assert.IsFalse(pickBanState.BansPublic);
            Assert.IsFalse(pickBanState.PicksPublic);
        }

        [TestMethod]
        public void SetBan_AddsBanCorrectly()
        {
            // Arrange
            var pickBanState = new PickBanState(Guid.NewGuid(), PickBanMode.Commanders, GameMode.Commanders, 2, 6);
            var ban = new PickBan { Slot = 1, Commander = Commander.Abathur, Locked = false };

            // Act
            var dto = pickBanState.SetBan(ban);

            // Assert
            Assert.IsNotNull(dto);
            Assert.AreEqual(1, pickBanState.Bans.Count);
            Assert.AreEqual(ban.Slot, pickBanState.Bans[0].Slot);
            Assert.AreEqual(ban.Commander, pickBanState.Bans[0].Commander);
            Assert.AreEqual(ban.Locked, pickBanState.Bans[0].Locked);
        }

        [TestMethod]
        public void SetPick_AddsPickCorrectly()
        {
            // Arrange
            var pickBanState = new PickBanState(Guid.NewGuid(), PickBanMode.Commanders, GameMode.Commanders, 2, 6);
            pickBanState.SetBan(new PickBan { Slot = 1, Commander = Commander.Abathur, Locked = true });
            pickBanState.SetBan(new PickBan { Slot = 2, Commander = Commander.Alarak, Locked = true });
            pickBanState.SetBan(new PickBan { Slot = 3, Commander = Commander.Artanis, Locked = true });
            var pick = new PickBan { Slot = 1, Commander = Commander.Stukov, Locked = true };

            // Act
            var dto = pickBanState.SetPick(pick);

            // Assert
            Assert.IsNotNull(dto);
            Assert.AreEqual(1, pickBanState.Picks.Count);
            Assert.AreEqual(pick.Slot, pickBanState.Picks[0].Slot);
            Assert.AreEqual(pick.Commander, pickBanState.Picks[0].Commander);
            Assert.AreEqual(pick.Locked, pickBanState.Picks[0].Locked);
        }
    }
}