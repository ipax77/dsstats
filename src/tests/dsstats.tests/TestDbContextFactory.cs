using Microsoft.EntityFrameworkCore;

namespace dsstats.tests;

internal sealed class TestDbContextFactory<TContext>(DbContextOptions<TContext> options) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext()
        => (TContext)Activator.CreateInstance(typeof(TContext), options)!;
}
