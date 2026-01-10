using dsstats.shared.Maui;
using Microsoft.AspNetCore.Components;

namespace dsstats.maui.Components.Pages
{
    public partial class ProfileEditor
    {
        [Parameter]
        public Sc2ProfileDto Profile { get; set; } = new();

        [Parameter]
        public EventCallback<Sc2ProfileDto> OnRemove { get; set; }

        private async Task OnRemoveClicked()
        {
            await OnRemove.InvokeAsync(Profile);
        }
    }
}
