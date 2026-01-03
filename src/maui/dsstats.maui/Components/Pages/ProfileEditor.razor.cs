using dsstats.db;
using Microsoft.AspNetCore.Components;

namespace dsstats.maui.Components.Pages
{
    public partial class ProfileEditor
    {
        [Parameter]
        public Sc2Profile Profile { get; set; } = new();

        [Parameter]
        public EventCallback<Sc2Profile> OnRemove { get; set; }

        private async Task OnRemoveClicked()
        {
            await OnRemove.InvokeAsync(Profile);
        }
    }
}
