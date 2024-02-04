using dsstats.web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace dsstats.web.Client.Pages.Auth;

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
    string testResponse = string.Empty;

    ErrorResponse? ErrorResponse = null;

    protected override void OnInitialized()
    {
        editContext = new(loginData);
        AuthenticationStateProvider.AuthenticationStateChanged += AuthStateChanged;
        base.OnInitialized();
    }

    //protected override async Task OnAfterRenderAsync(bool firstRender)
    //{
    //    if (firstRender)
    //    {
    //        await ((ExternalAuthStateProvider)AuthenticationStateProvider)
    //            .TryLogin();
    //    }
    //    await base.OnAfterRenderAsync(firstRender);
    //}

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            //_ = ((ExternalAuthStateProvider)AuthenticationStateProvider)
            //    .TryLogin();
        }
        base.OnAfterRender(firstRender);
    }

    private async void AuthStateChanged(Task<AuthenticationState> task)
    {
        //var state = task.Result;
        //if (state?.User.Identity is not null 
        //     && state.User.Identity.IsAuthenticated
        //     && !string.IsNullOrEmpty(RedirectUrl))
        //{
        //    NavigationManager.NavigateTo(RedirectUrl);
        //}

        AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated ?? false
            && !string.IsNullOrEmpty(RedirectUrl))
        {
            NavigationManager.NavigateTo(RedirectUrl!);
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

    private async Task ForgotPassword()
    {
        if (!editContext.Validate())
        {
            return;
        }

        var errorResponse = await ((ExternalAuthStateProvider)AuthenticationStateProvider)
            .ForgotPassword(loginData.Email);
        
        if (errorResponse.Status == 200)
        {
            ErrorResponse = errorResponse with { Title = "Please check your emails for the reset code." };
        }
        else
        {
            ErrorResponse = errorResponse;
        }
    }

    private void RegisterNew()
    {

    }

    private void ResendEmail()
    {

    }

    private async Task TestRequest()
    {
        var httpClient = await ((ExternalAuthStateProvider)AuthenticationStateProvider).GetApiHttpClient();
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