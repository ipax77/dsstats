using dsstats.api.AuthContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace dsstats.api.Services;

public static class DsPolicy
{
    public const string TourneyManager = nameof(TourneyManager);
    public const string Admin = nameof(Admin);
}

public class UserRepository(RoleManager<IdentityRole> roleManager,
                            UserManager<DsUser> userManager,
                            IConfiguration configuration,
                            IEmailSender emailSender,
                            ILogger<UserRepository> logger)
{
    public async Task Seed()
    {
        await SeedRoles();
        await SeedUserRoles();
    }

    private async Task SeedRoles()
    {
        List<string> roles = ["Admin", "Tourney"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private async Task SeedUserRoles()
    {
        var userRoles = GetUserRoles();
        foreach (var ent in userRoles)
        {
            var user = await userManager.FindByEmailAsync(ent.Key);
            if (user is null)
            {
                continue;
            }
            foreach (var role in ent.Value)
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }

    private Dictionary<string, List<string>> GetUserRoles()
    {
        var userRolesConfig = configuration.GetSection("ServerConfig:Auth:UserRoles");
        var userRoles = userRolesConfig.Get<Dictionary<string, List<string>>>();
        return userRoles ?? new();
    }

    public async Task<bool> ChangeName(ClaimsPrincipal user, string newName)
    {
        var dsUser = await userManager.GetUserAsync(user);

        if (dsUser is null)
        {
            return false;
        }

        var result = await userManager.SetUserNameAsync(dsUser, newName);

        if (result.Errors.Any())
        {
            logger.LogError("failed changing username: {errors}", string.Join(", ", result.Errors));
        }

        return result.Succeeded;
    }

    public async Task<bool> ChangeEmail(ClaimsPrincipal user, string newEmail, string token)
    {
        var dsUser = await userManager.GetUserAsync(user);

        if (dsUser is null)
        {
            return false;
        }

        var result = await userManager.ChangeEmailAsync(dsUser, newEmail, token);

        if (result.Errors.Any())
        {
            logger.LogError("failed changing email: {errors}", string.Join(", ", result.Errors));
        }

        return result.Succeeded;
    }

    public async Task<bool> SendEmailChangeToken(ClaimsPrincipal user, string newEmail)
    {
        var dsUser = await userManager.GetUserAsync(user);

        if (dsUser is null)
        {
            return false;
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(dsUser, newEmail);

        string htmlMessage = @$"Please click on the following link to confirm your Email change.
<a href=""https://dsstats.pax77.org/auth/changeemail?email={newEmail}?token={token}"">Confirm</a>";

        await emailSender.SendEmailAsync(newEmail, "Confirm changed Email address", htmlMessage);

        return true;
    }

    public async Task<bool> DeleteUser(ClaimsPrincipal user)
    {
        var dsUser = await userManager.GetUserAsync(user);

        if (dsUser is null)
        {
            return true;
        }

        var result = await userManager.DeleteAsync(dsUser);

        if (result.Errors.Any())
        {
            logger.LogError("failed changing email: {errors}", string.Join(", ", result.Errors));
        }

        return result.Succeeded;
    }
}

public class DsClaimsFactory : IUserClaimsPrincipalFactory<DsUser>
{
    public Task<ClaimsPrincipal> CreateAsync(DsUser user)
    {
        var claims = new Claim[] {
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.Name, user.UserName ?? ""),
    };

        var claimsIdentity = new ClaimsIdentity(claims, "BearerToken");

        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        return Task.FromResult(claimsPrincipal);

    }
}