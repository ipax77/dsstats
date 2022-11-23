
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    public Dictionary<RequestNames, MmrProgress> MauiMmrProgress { get; private set; } = new();
    private Dictionary<int, RequestNames> toonIdmauiPlayers = new();
    public bool IsProgressActive;


    private void SetProgress(ReplayData processData, bool std)
    {
        foreach (var playerData in processData.WinnerTeamData.Players)
        {
            if (std)
            {
                SetStdProgress(playerData);
            }
            else
            {
                SetCmdrProgress(playerData);
            }
        }
        foreach (var playerData in processData.LoserTeamData.Players)
        {
            if (std)
            {
                SetStdProgress(playerData);
            }
            else
            {
                SetCmdrProgress(playerData);
            }
        }
    }

    private void SetCmdrProgress(PlayerData playerData)
    {
        if (toonIdmauiPlayers.ContainsKey(playerData.ReplayPlayer.Player.ToonId))
        {
            var requestNames = toonIdmauiPlayers[playerData.ReplayPlayer.Player.ToonId];

            if (MauiMmrProgress.ContainsKey(requestNames))
            {
                var mmrProgress = MauiMmrProgress[requestNames];
                mmrProgress.CmdrMmrDeltas.Add(playerData.DeltaPlayerMmr);
                if (mmrProgress.CmdrMmrStart == 0)
                {
                    mmrProgress.CmdrMmrStart = ToonIdRatings.ContainsKey(playerData.ReplayPlayer.Player.ToonId) ?
                        ToonIdRatings[playerData.ReplayPlayer.Player.ToonId].CmdrRatingStats.Mmr
                    : startMmr;
                }
            }
            else
            {
                MauiMmrProgress[requestNames] = new()
                {
                    CmdrMmrStart = ToonIdRatings.ContainsKey(playerData.ReplayPlayer.Player.ToonId) ?
                        ToonIdRatings[playerData.ReplayPlayer.Player.ToonId].CmdrRatingStats.Mmr
                        : startMmr,
                    CmdrMmrDeltas = new() { playerData.DeltaPlayerMmr }
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
                mmrProgress.StdMmrDeltas.Add(playerData.DeltaPlayerMmr);
                if (mmrProgress.StdMmrStart == 0)
                {
                    mmrProgress.StdMmrStart = ToonIdRatings.ContainsKey(playerData.ReplayPlayer.Player.ToonId) ?
                        ToonIdRatings[playerData.ReplayPlayer.Player.ToonId].StdRatingStats.Mmr
                    : startMmr;
                }
            }
            else
            {
                MauiMmrProgress[requestNames] = new()
                {
                    StdMmrStart = ToonIdRatings.ContainsKey(playerData.ReplayPlayer.Player.ToonId) ?
                        ToonIdRatings[playerData.ReplayPlayer.Player.ToonId].StdRatingStats.Mmr
                        : startMmr,
                    StdMmrDeltas = new() { playerData.DeltaPlayerMmr }
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
