using Integration.BusinessLogics.Nps.Abstractions;
using Integration.BusinessLogics.Nps.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nibbs.Nps.Integration;
using Nibbs.Nps.Integration.Messages.Acmt;

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

    /// <summary>
    /// Enables the inbound half of the NPS Identification Verification flow: inbound
    /// acmt.023 name enquiries are resolved through <typeparamref name="TAccountVerificationService"/>
    /// and answered with an acmt.024 report. Requires AddIntegrationBusinessLogics()
    /// and AddNpsIntegration().
    /// </summary>
    /// <typeparam name="TAccountVerificationService">
    /// The institution's account lookup, implemented against the core banking system.
    /// </typeparam>
    public static IServiceCollection AddNpsIdentificationVerificationFlow<TAccountVerificationService>(
        this IServiceCollection services)
        where TAccountVerificationService : class, IAccountVerificationService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IAccountVerificationService, TAccountVerificationService>();
        services.AddNpsMessageHandler<InboundIdVerificationRequestHandler, Acmt023Document>();
        return services;
    }
}
