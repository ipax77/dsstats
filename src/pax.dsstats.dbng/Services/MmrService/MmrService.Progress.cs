
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    public Dictionary<RequestNames, MmrProgress> MauiMmrProgress { get; private set; } = new();
    private Dictionary<int, RequestNames> toonIdmauiPlayers = new();
    public bool IsProgressActive;

    private void SetCmdrProgress(PlayerData playerData)
    {
        if (toonIdmauiPlayers.ContainsKey(playerData.ReplayPlayer.Player.ToonId))
        {
            var requestNames = toonIdmauiPlayers[playerData.ReplayPlayer.Player.ToonId];

            if (MauiMmrProgress.ContainsKey(requestNames))
            {
                var mmrProgress = MauiMmrProgress[requestNames];
                mmrProgress.CmdrMmrDeltas.Add(playerData.PlayerMmrDelta);
                if (mmrProgress.CmdrMmrStart == 0)
                {
                    mmrProgress.CmdrMmrStart = ToonIdRatings.ContainsKey(playerData.ReplayPlayer.Player.ToonId) ?
                        ToonIdRatings[playerData.ReplayPlayer.Player.ToonId].CmdrRatingStats.Mmr - playerData.PlayerMmrDelta
                    : startMmr;
                }
            }
            else
            {
                MauiMmrProgress[requestNames] = new()
                {
                    CmdrMmrStart = ToonIdRatings.ContainsKey(playerData.ReplayPlayer.Player.ToonId) ?
                        ToonIdRatings[playerData.ReplayPlayer.Player.ToonId].CmdrRatingStats.Mmr - playerData.PlayerMmrDelta
                        : startMmr,
                    CmdrMmrDeltas = new() { playerData.PlayerMmrDelta }
                };
            }
        }
    }

    private void SetStdProgress(PlayerData playerData)
    {
        if (toonIdmauiPlayers.ContainsKey(playerData.ReplayPlayer.Player.ToonId))
        {
            var requestNames = toonIdmauiPlayers[playerData.ReplayPlayer.Player.ToonId];

            if (MauiMmrProgress.ContainsKey(requestNames))
            {
                var mmrProgress = MauiMmrProgress[requestNames];
                mmrProgress.StdMmrDeltas.Add(playerData.PlayerMmrDelta);
                if (mmrProgress.StdMmrStart == 0)
                {
                    mmrProgress.StdMmrStart = ToonIdRatings.ContainsKey(playerData.ReplayPlayer.Player.ToonId) ?
                        ToonIdRatings[playerData.ReplayPlayer.Player.ToonId].StdRatingStats.Mmr - playerData.PlayerMmrDelta
                    : startMmr;
                }
            }
            else
            {
                MauiMmrProgress[requestNames] = new()
                {
                    StdMmrStart = ToonIdRatings.ContainsKey(playerData.ReplayPlayer.Player.ToonId) ?
                        ToonIdRatings[playerData.ReplayPlayer.Player.ToonId].StdRatingStats.Mmr - playerData.PlayerMmrDelta
                        : startMmr,
                    StdMmrDeltas = new() { playerData.PlayerMmrDelta }
                };
            }
        }
    }

    public void DEBUG_SeedProgress()
    {
        var requestName = new RequestNames()
        {
            Name = "PAX",
            ToonId = 1
        };

        MauiMmrProgress[requestName] = new()
        {
            CmdrMmrStart = 1600,
            CmdrMmrDeltas = new()
            {
                20,
                30,
                -10,
                -7.7,
                8.2,
                20,
                30,
                -10,
                -7.7,
                8.2,
                20,
                30,
                -10,
                -7.7,
                8.2,
                20,
                30,
                -10,
                -7.7,
                8.2
            }
        };
    }
}
