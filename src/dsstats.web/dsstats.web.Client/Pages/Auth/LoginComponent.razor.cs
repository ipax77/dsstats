using dsstats.web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace dsstats.web.Client.Pages.Auth;

public partial class LoginComponent : ComponentBase, IDisposable
{
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    EditContext editContext = null!;
    LoginData loginData = new();
    string testResponse = string.Empty;

    protected override void OnInitialized()
    {
        editContext = new(loginData);
        AuthenticationStateProvider.AuthenticationStateChanged += AuthStateChanged;
        base.OnInitialized();
    }

    private void AuthStateChanged(Task<AuthenticationState> task)
    {
        InvokeAsync(() => StateHasChanged());
    }

    private async Task Login()
    {
        await ((ExternalAuthStateProvider)AuthenticationStateProvider)
            .LogInAsync(loginData.Email, loginData.Password);
    }

    private async Task TestRequest()
    {
        var httpClient = ((ExternalAuthStateProvider)AuthenticationStateProvider).GetApiHttpClient();
        try
        {
            var response = await httpClient.GetAsync("api8/v1/managetourney/test1");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            testResponse = content;
            await InvokeAsync(() => StateHasChanged());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    internal record LoginData
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public void Dispose()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= AuthStateChanged;
    }
}