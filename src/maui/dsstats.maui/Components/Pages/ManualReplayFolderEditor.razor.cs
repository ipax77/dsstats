using dsstats.shared.Maui;
using Microsoft.AspNetCore.Components;

namespace dsstats.maui.Components.Pages
{
    public partial class ManualReplayFolderEditor
    {
        [Parameter]
        public MauiReplayFolderDto Folder { get; set; } = new();

        [Parameter]
        public EventCallback<MauiReplayFolderDto> OnRemove { get; set; }

        private async Task OnRemoveClicked()
        {
            await OnRemove.InvokeAsync(Folder);
        }
    }
}
