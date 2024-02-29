
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace dsstats.auth;

public class DsUser : IdentityUser
{
    public DsUser()
    {
        Profiles = new HashSet<DsProfile>();
    }
    public virtual ICollection<DsProfile> Profiles { get; set; }
}

public class DsProfile
{
    public int DsProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ToonId { get; set; }
    public int RealmId { get; set; }
    public int RegionId { get; set; }
}

public class DsRole : IdentityRole
{
    public DsRole()
    {
        UserRoles = new HashSet<DsRole>();
    }
    public virtual ICollection<DsRole> UserRoles { get; set; }
}

public class DsAuthContext : IdentityDbContext<DsUser>
{
    public DsAuthContext(DbContextOptions<DsAuthContext> options)
     : base(options)
    {
    }
}

