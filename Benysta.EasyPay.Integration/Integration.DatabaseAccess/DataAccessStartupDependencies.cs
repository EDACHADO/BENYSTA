using Integration.DatabaseAccess.Concrete;
using Integration.DatabaseAccess.Context;
using Integration.Infrastructures.Abstractions;
using Integration.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Integration.DatabaseAccess;

public static class DataAccessStartupDependencies
{
    public static IServiceCollection AddDALApplicationDependencies(this IServiceCollection services, IConfiguration Configuration)
    {
        // Add DbContext
        services.AddDbContext<NibssNpsDbContext>(options =>
          options.UseNpgsql(Configuration.GetConnectionString(DataAccessConstants.NibssNpsConnectionName)));


        services.AddScoped(typeof(IRepo<>), typeof(BaseRepo<>));

        // NIBSS NPS request/response log stores.
        services.AddScoped<ISingleTransferLogStore, SingleTransferLogStore>();
        services.AddScoped<INameEnquiryLogStore, NameEnquiryLogStore>();

        // Register services
        services.AddScoped(_ => new List<DatabaseAuditLog>());
        services.AddSingleton<AuditLogProcessor>();
        services.AddHostedService(provider => provider.GetRequiredService<AuditLogProcessor>());

        return services;
    }
}