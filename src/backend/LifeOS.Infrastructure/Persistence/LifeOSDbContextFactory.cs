using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LifeOS.Infrastructure.Persistence;

public class LifeOSDbContextFactory : IDesignTimeDbContextFactory<LifeOSDbContext>
{
    public LifeOSDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LifeOSDbContext>();
        
        // Use a dummy connection string for design-time operations
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=lifeos;Username=postgres;Password=postgres",
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(LifeOSDbContext).Assembly.FullName);
            });

        return new LifeOSDbContext(optionsBuilder.Options);
    }
}
