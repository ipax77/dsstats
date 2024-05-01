using dsstats.shared;

namespace dsstats.api.Services;

public partial class IhService
{
    private void SetStats(GroupState groupState, List<IhReplay> replays)
    {

    }

    private void SetReplayStat(GroupState groupState, IhReplay replay)
    {
        foreach (var player in replay.Metadata.Players)
        {
            var groupPlayer = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player.PlayerId);

            if (groupPlayer is null)
            {
                groupPlayer = new PlayerState()
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                };
                groupState.PlayerStates.Add(groupPlayer);
            }

            if (player.Observer)
            {
                groupPlayer.Observer++;
            }
            else
            {
                groupPlayer.Games++;
            }
        }

        foreach (var player in replay.Replay.ReplayPlayers)
        {
            PlayerId playerId = new(player.Player.ToonId, player.Player.RealmId, player.Player.RegionId);
            var groupPlayer = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == playerId);

            if (groupPlayer is null)
            {
                groupPlayer = new PlayerState()
                {
                    PlayerId = playerId
                };
                groupState.PlayerStates.Add(groupPlayer);
            }
            groupPlayer.Name = player.Name;

            foreach (var otherPlayer in replay.Replay.ReplayPlayers)
            {
                if (otherPlayer == player)
                {
                    continue;
                }

                PlayerId otherPlayerId = new(otherPlayer.Player.ToonId, otherPlayer.Player.RealmId, otherPlayer.Player.RegionId);
                if (player.Team == otherPlayer.Team)
                {
                    groupPlayer.PlayedWith.Add(otherPlayerId);
                }
                else
                {
                    groupPlayer.PlayedAgainst.Add(otherPlayerId);
                }
            }
        }
    }
}
