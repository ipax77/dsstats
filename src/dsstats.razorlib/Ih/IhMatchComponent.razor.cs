using dsstats.shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace dsstats.razorlib.Ih;

public partial class IhMatchComponent : ComponentBase
{
    [Inject]
    public ILogger<IhMatchComponent> Logger { get; set; } = default!;

    IhMatch match = new();

    List<PlayerState> playerStates = [
        new() {
            PlayerId = new(1, 1, 2),
            Name = "Test1",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(2, 1, 2),
            Name = "Test2",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(3, 1, 2),
            Name = "Test3",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(4, 1, 2),
            Name = "Test4",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(5, 1, 2),
            Name = "Test5",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(6, 1, 2),
            Name = "Test6",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(7, 1, 2),
            Name = "Test7",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(8, 1, 2),
            Name = "Test8",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(9, 1, 2),
            Name = "Test9",
            RatingStart = 1000
        },
        new() {
            PlayerId = new(10, 1, 2),
            Name = "Test10",
            RatingStart = 1000
        },
    ];

    List<PlayerState> availablePlayers = [];
    PlayerId? dragPlayerId = null;
    int dragStartTeam = -1;

    protected override void OnInitialized()
    {
        availablePlayers = new(playerStates);
        PlayerState[] players = playerStates.ToArray();
        Random.Shared.Shuffle(players);

        for (int i = 0; i < 6; i++)
        {
            var team = i < 3 ? match.Teams[0] : match.Teams[1];
            var pos = i < 3 ? i : i - 3;
            var player = players[i];
            availablePlayers.Remove(player);
            team.Slots[pos].PlayerId = player.PlayerId;
            team.Slots[pos].Name = player.Name;
            team.Slots[pos].Rating = player.RatingStart;
        }
        base.OnInitialized();
    }

    private void HandleListDrop(DragEventArgs e)
    {
        if (dragStartTeam > 0 && dragPlayerId is not null)
        {
            var team = match.Teams[dragStartTeam];
            var slot = team.Slots.FirstOrDefault(f => f.PlayerId == dragPlayerId);
            if (slot is not null)
            {
                slot.PlayerId = new();
                slot.Name = string.Empty;
                slot.Rating = 0;
                var player = availablePlayers.FirstOrDefault(f => f.PlayerId == dragPlayerId);
                if (player is not null)
                {
                    availablePlayers.Add(player);
                }
                dragStartTeam = -1;
                dragPlayerId = null;
            }
        }
        else
        {
            dragStartTeam = -1;
            dragPlayerId = null;
        }
        StateHasChanged();
    }

    private void HandleListDragEnter(DragEventArgs e)
    {
    }

    private void HandleListDragLeave(DragEventArgs e)
    {
    }

    private void HandleListDragStart(PlayerId playerId)
    {
        dragPlayerId = playerId;
        dragStartTeam = 0;
    }

    private void HandleTeamDrop(DragEventArgs e, int team, PlayerId playerId)
    {
        if (team == dragStartTeam)
        {
            dragStartTeam = -1;
            dragPlayerId = null;
            return;
        }

        var mteam = match.Teams[team];
        var slot = mteam.Slots.FirstOrDefault(f => f.PlayerId == playerId);

        if (slot is null)
        {
            dragStartTeam = -1;
            dragPlayerId = null;
            return;
        }

        if (dragStartTeam == 0)
        {
            var availablePlayer = availablePlayers.FirstOrDefault(f => f.PlayerId == playerId);
            if (availablePlayer is null)
            {
                dragStartTeam = -1;
                dragPlayerId = null;
                return;
            }
            availablePlayers.Remove(availablePlayer);
            var slotPlayer = playerStates.FirstOrDefault(f => f.PlayerId == slot.PlayerId);

            slot.PlayerId = availablePlayer.PlayerId;
            slot.Name = availablePlayer.Name;
            slot.Rating = availablePlayer.RatingStart;

            if (slotPlayer is not null)
            {
                availablePlayers.Add(slotPlayer);
            }
        }
        else
        {
            var player = playerStates.FirstOrDefault(f => f.PlayerId == dragPlayerId);
            var oldSlot = match.Teams[dragStartTeam].Slots.FirstOrDefault(f => f.PlayerId == dragPlayerId);
            
            if (player is null || oldSlot is null)
            {
                dragStartTeam = -1;
                dragPlayerId = null;
                return;
            }

            oldSlot.PlayerId = slot.PlayerId;
            oldSlot.Name = slot.Name;
            oldSlot.Rating = slot.Rating;

            slot.PlayerId = player.PlayerId;
            slot.Name = player.Name;
            slot.Rating = player.RatingStart;
        }

        dragStartTeam = -1;
        dragPlayerId = null;
        StateHasChanged();
    }

    private void HandleTeamDragEnter(DragEventArgs e, int team, PlayerId playerId)
    {
    }

    private void HandleTeamDragLeave(DragEventArgs e, int team, PlayerId playerId)
    {
    }

    private void HandleTeamDragStart(int team, PlayerId playerId)
    {
        dragStartTeam = team;
        dragPlayerId = playerId;
    }

    private string GetId(PlayerId playerId)
    {
        return $"{playerId.ToonId}|{playerId.RealmId}|{playerId.RegionId}";
    }
}