using Microsoft.EntityFrameworkCore;
using EntityFrameworkCore.CreatedUpdatedDate.Extensions;
using OsitoPolar.IAM.Service.Infrastructure.Persistence.EFC.Configuration.Extensions;
using OsitoPolar.IAM.Service.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace OsitoPolar.IAM.Service.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
/// IAM Bounded Context database context
/// </summary>
public class IAMDbContext(DbContextOptions<IAMDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        // Add the created and updated interceptor
        builder.AddCreatedUpdatedInterceptor();
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply IAM context configuration
        builder.ApplyIamConfiguration();

        // Apply snake_case naming convention
        builder.UseSnakeCaseNamingConvention();
    }
}
