using dsstats.api.AuthContext;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace dsstats.api.Services;

public static class DsPolicy
{
    public const string TourneyManager = nameof(TourneyManager);
    public const string Admin = nameof(Admin);
}

public class UserRepository(RoleManager<IdentityRole> roleManager, UserManager<DsUser> userManager, IConfiguration configuration)
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