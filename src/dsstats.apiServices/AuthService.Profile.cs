
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Web;

namespace dsstats.apiServices;

public partial class AuthService
{
    private readonly string userController = "api8/v1/DsUser";

    public async Task<bool> IsUserInRole(string role)
    {
        try
        {
            var encodedRole = HttpUtility.HtmlEncode(role);
            var response = await httpClient.GetAsync($"{userController}/isinrole/{encodedRole}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting isUserInRole result: {error}", ex.Message);
        }
        return false;
    }

    public async Task<bool> RequestNewEmail(string newEmail)
    {
        try
        {
            var encodedEmail = HttpUtility.HtmlEncode(newEmail);
            var response = await httpClient.GetAsync($"{userController}/requestnewemail/{encodedEmail}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed requesting newEmail: {error}", ex.Message);
        }
        return false;
    }

    public async Task<bool> ChangeEmail(string newEmail, string token)
    {
        try
        {
            var encodedEmail = HttpUtility.HtmlEncode(newEmail);
            var encodedToken = HttpUtility.HtmlEncode(token);
            var response = await httpClient.GetAsync($"{userController}/changeemail/{encodedEmail}/{encodedToken}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed changing email: {error}", ex.Message);
        }
        return false;
    }

    public async Task<bool> ChangeUserName(string newName)
    {
        try
        {
            var encodedName = HttpUtility.HtmlEncode(newName);
            var response = await httpClient.GetAsync($"{userController}/changename/{encodedName}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed changing username: {error}", ex.Message);
        }
        return false;
    }

    public async Task<bool> Delete()
    {
        try
        {
            var response = await httpClient.GetAsync($"{userController}/delete");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed deleting user: {error}", ex.Message);
        }
        return false;
    }
}
