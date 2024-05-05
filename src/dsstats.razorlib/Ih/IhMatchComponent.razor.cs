using dsstats.shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace dsstats.razorlib.Ih;

public partial class IhMatchComponent : ComponentBase
{
    [Inject]
    public ILogger<IhMatchComponent> Logger { get; set; } = default!;

    [CascadingParameter]
    public GroupState GroupState { get; set; } = default!;

    [Parameter]
    public EventCallback OnAddPlayersRequest { get; set; } = default!;

    private List<PlayerState> availablePlayers => GroupState.PlayerStates
        .Where(x => !GroupState.IhMatch.Teams.Any(a => a.Slots.Any(a => a.PlayerId == x.PlayerId)))
        .ToList();

    private DropContainer dropContainer = new();

    private void CreateIhMatch()
    {
        GroupState.CreateMatch();
        StateHasChanged();
    }

    private void HandleListDrop(DragEventArgs e)
    {
        if (dropContainer.PlayerId is null || dropContainer.Team == -1)
        {
            dropContainer.Reset();
            return;
        }

        var ihteam = GroupState.IhMatch.Teams[dropContainer.Team];
        var ihslot = ihteam.Slots.FirstOrDefault(f => f.PlayerId == dropContainer.PlayerId);

        if (ihslot is null)
        {
            dropContainer.Reset();
            return;
        }

        ihslot.PlayerId = new();
        ihslot.Name = "Empty";
        ihslot.Rating = 0;

        dropContainer.Reset();
        GroupState.IhMatch.SetScores(GroupState);
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
        dropContainer.PlayerId = playerId;
        dropContainer.Team = -1;
    }

    private void HandleTeamDrop(DragEventArgs e, int team, PlayerId playerId)
    {
        if (dropContainer.PlayerId is null || dropContainer.Team == team)
        {
            dropContainer.Reset();
            return;
        }

        var ihteam = GroupState.IhMatch.Teams[team];
        var ihslot = ihteam.Slots.FirstOrDefault(f => f.PlayerId ==  playerId);

        var player = GroupState.PlayerStates.FirstOrDefault(f => f.PlayerId == dropContainer.PlayerId);

        if (ihslot is null || player is null)
        {
            dropContainer.Reset();
            return;
        }

        if (dropContainer.Team == -1)
        {
            ihslot.PlayerId = player.PlayerId;
            ihslot.Name = player.Name;
            ihslot.Rating = player.RatingStart;
        }
        else
        {
            var oldslot = GroupState.IhMatch.Teams[dropContainer.Team].Slots
                .FirstOrDefault(f => f.PlayerId == dropContainer.PlayerId);
            var oldplayer = GroupState.PlayerStates.FirstOrDefault(f => f.PlayerId == dropContainer.PlayerId);

            if (oldslot is null || oldplayer is null)
            {
                dropContainer.Reset();
                return;
            }
            oldslot.PlayerId = ihslot.PlayerId;
            oldslot.Name = ihslot.Name;
            oldslot.Rating = ihslot.Rating;

            ihslot.PlayerId = oldplayer.PlayerId;
            ihslot.Name = oldplayer.Name;
            ihslot.Rating = oldplayer.RatingStart;
        }
        dropContainer.Reset();
        GroupState.IhMatch.SetScores(GroupState);
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
        dropContainer.PlayerId = playerId;
        dropContainer.Team = team;
    }

    private static string GetId(PlayerId playerId)
    {
        return $"{playerId.ToonId}|{playerId.RealmId}|{playerId.RegionId}";
    }

    private record DropContainer
    {
        public PlayerId? PlayerId { get; set; }
        public int Team { get; set; } = -2;
        public void Reset()
        {
            PlayerId = null;
            Team = -2;
        }
    }
}