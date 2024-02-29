using dsstats.authclient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace dsstats.authclient;

public partial class LoginComponent : ComponentBase, IDisposable
{
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;


    [SupplyParameterFromQuery]
    public string? RedirectUrl { get; set; }

    EditContext editContext = null!;
    LoginData loginData = new();

    protected override void OnInitialized()
    {
        editContext = new(loginData);
        AuthenticationStateProvider.AuthenticationStateChanged += AuthStateChanged;
        base.OnInitialized();
    }

    private async void AuthStateChanged(Task<AuthenticationState> task)
    {
        //var state = task.Result;
        var state = await task;
        
        // AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        if (state.User.Identity?.IsAuthenticated ?? false)
        {
            if (!string.IsNullOrEmpty(RedirectUrl))
            {
                NavigationManager.NavigateTo(RedirectUrl!);
            }
        }

        await InvokeAsync(() => StateHasChanged());
    }

    private void TryLogin()
    {
        _ = ((ExternalAuthStateProvider)AuthenticationStateProvider)
            .TryLogin();
    }

    private void Login()
    {
        _ = ((ExternalAuthStateProvider)AuthenticationStateProvider)
          .LogInAsync(loginData.Email, loginData.Password, loginData.Remember);
    }

    private void ForgotPassword()
    {
        NavigationManager.NavigateTo("auth/reset");
    }

    private void RegisterNew()
    {
        NavigationManager.NavigateTo("auth/register");
    }

    private void ResendEmail()
    {
        NavigationManager.NavigateTo("auth/resendemail");
    }

    internal record LoginData
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ResetCode {  get; set; } = string.Empty;
        public bool Remember { get; set; }
    }

    public void Dispose()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= AuthStateChanged;
    }
}