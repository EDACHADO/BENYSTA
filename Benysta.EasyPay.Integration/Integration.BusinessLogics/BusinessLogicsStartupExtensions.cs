using Microsoft.Extensions.DependencyInjection;

namespace Integration.BusinessLogics;

public static class BusinessLogicsStartupExtensions
{
    /// <summary>
    /// Registers the business-logic layer: MediatR and every command/query handler
    /// in this assembly. The NPS integration services these handlers depend on must
    /// be registered separately (services.AddNpsIntegration()).
    /// </summary>
    public static IServiceCollection AddIntegrationBusinessLogics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(static configuration =>
            configuration.RegisterServicesFromAssembly(typeof(BusinessLogicsStartupExtensions).Assembly));

        return services;
    }
}
