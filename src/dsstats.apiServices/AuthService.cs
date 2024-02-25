using dsstats.shared.Auth;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;



public class AuthService(HttpClient httpClient, ILogger<AuthService> logger) : IAuthService
{
    private readonly string authController = "/account";

    public async Task<IRegisterResponse> Register(RegisterPayload register)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{authController}/register", register);
            if (response.IsSuccessStatusCode)
            {
                return new RegisterSuccessResponse();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                if (error is not null)
                {
                    return new RegisterFailedResponse() { Error = error };
                }
                else
                {
                    return new RegisterFailedResponse() { Error = new() { Status = 400, Detail = "Register failed." } };
                }
            }
            else
            {
                return new RegisterFailedResponse() { Error = new() { Status = (int)response.StatusCode, Detail = "Register failed." } };
            }
        }
        catch (Exception ex)
        {
            return new RegisterFailedResponse() { Error = new() { Status = 500, Detail = ex.Message } };
        }
    }

    public async Task<ILoginResponse> Login(LoginPayload login)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{authController}/login", login);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TokenInfo>();
            if (result is not null)
            {
                return new LoginSucessResponse() { TokenInfo = result };
            }
            else
            {
                return new LoginFailedResponse() { Error = "Token info was null." };
            }
        }
        catch (Exception ex)
        {
            return new LoginFailedResponse() { Error = ex.Message };
        }
    }

    public async Task<ILoginResponse> Refresh(string refreshToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{authController}/refresh", new { RefreshToken = refreshToken });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TokenInfo>();
            if (result is not null)
            {
                return new LoginSucessResponse() { TokenInfo = result };
            }
            else
            {
                return new LoginFailedResponse { Error = "Tokeninfo was null." };
            }
        }
        catch (Exception ex)
        {
            return new LoginFailedResponse { Error = ex.Message };
        }
    }

    public async Task<bool> ResendConfirmationEmail(string email)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{authController}/resendConfirmationEmail", new { Email = email });
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("failed email resend request: {error}", ex.Message);
            return false;
        }
    }

    public async Task<IForgotPasswordResponse> ForgotPassword(string email)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{authController}/forgotPassword", new { Email = email });
            if (response.IsSuccessStatusCode)
            {
                return new ForgotPasswordSuccessResponse();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                if (result is not null)
                {
                    return new ForgotPasswordFailedResponse() { Error = result };
                }
                else
                {
                    return new ForgotPasswordFailedResponse() { Error = new() { Status = 400, Detail = "Error was null." } };
                }
            }
            else
            {
                return new ForgotPasswordFailedResponse() { Error = new() { Status = (int)response.StatusCode, Detail = "ForgotPassword failed." } };
            }
        }
        catch (Exception ex)
        {
            return new ForgotPasswordFailedResponse() { Error = new() { Status = 500, Detail = ex.Message } };
        }
    }

    public async Task<IForgotPasswordResponse> ResetPassword(ResetPaylaod reset)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{authController}/resetPassword", reset);
            if (response.IsSuccessStatusCode)
            {
                return new ForgotPasswordSuccessResponse();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                if (result is not null)
                {
                    return new ForgotPasswordFailedResponse() { Error = result };
                }
                else
                {
                    return new ForgotPasswordFailedResponse() { Error = new() { Status = 400, Detail = "Error was null." } };
                }
            }
            else
            {
                return new ForgotPasswordFailedResponse() { Error = new() { Status = (int)response.StatusCode, Detail = "Reset password failed." } };
            }
        }
        catch (Exception ex)
        {
            return new ForgotPasswordFailedResponse() { Error = new() { Status = 500, Detail = ex.Message } };
        }
    }

    public async Task<IManageInfoResponse> ManageInfo(ManageInfoPayload manage)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{authController}/manage/info", manage);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ManageInfoResponse>();
                if (result is not null)
                {
                    return new ManageInfoSuccessResponse() { Response = result };
                }
                else
                {
                    return new ManageInfoFailedResponse() { Error = new() { Status = 500, Detail = "Manage result was null." } };
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                if (result is not null)
                {
                    return new ManageInfoFailedResponse() { Error = result };
                }
                else
                {
                    return new ManageInfoFailedResponse { Error = new() { Status = 400, Detail = "Manage info failed." } };
                }
            }
            else
            {
                return new ManageInfoFailedResponse { Error = new() { Status = (int)response.StatusCode, Detail = "Manage info failed." } };
            }
        }
        catch (Exception ex)
        {
            return new ManageInfoFailedResponse() { Error = new() { Status = 500, Detail = ex.Message } };
        }
    }
}
