
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace dsstats.api.AuthContext;

public class DsUser : IdentityUser
{

}

public class DsAuthContext : IdentityDbContext<DsUser>
{
    public DsAuthContext(DbContextOptions<DsAuthContext> options)
     : base(options)
    {
    }
}

