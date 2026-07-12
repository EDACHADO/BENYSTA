using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Client;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Cryptography;
using Nibbs.Nps.Integration.Inbound;
using Nibbs.Nps.Integration.Messages;

namespace Nibbs.Nps.Integration;

public static class NibbsNpsStartupExtensions
{
    /// <summary>
    /// Registers the NPS integration services: key provider, message protector,
    /// message factory, inbound processor and the <see cref="INpsApiClient"/> HTTP client.
    /// </summary>
    public static IServiceCollection AddNpsIntegration(
        this IServiceCollection services,
        Action<NpsOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services.AddNpsIntegrationCore();
    }

    /// <summary>
    /// Registers the NPS integration services, expecting <see cref="NpsOptions"/> to be
    /// configured elsewhere (e.g. services.Configure&lt;NpsOptions&gt;(configuration.GetSection(NpsOptions.SectionName))).
    /// </summary>
    public static IServiceCollection AddNpsIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddNpsIntegrationCore();
    }

    /// <summary>
    /// Additionally registers the <see cref="INibssInstitutionGatewayClient"/> for the
    /// NIBSS Institution request-message service on the API Gateway.
    /// </summary>
    public static IServiceCollection AddNibssInstitutionGateway(
        this IServiceCollection services,
        Action<NpsGatewayOptions> configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions is not null)
            services.Configure(configureOptions);

        services.TryAddSingleton<INpsKeyProvider, NpsPemKeyProvider>();
        services.TryAddSingleton<INpsMessageProtector, NpsMessageProtector>();

        services.AddHttpClient<INibssInstitutionGatewayClient, NibssInstitutionGatewayClient>(
            static (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<NpsGatewayOptions>>().Value;
                client.Timeout = options.Timeout;
            });

        return services;
    }

    /// <summary>
    /// Registers an <see cref="INpsMessageHandler{TDocument}"/> implementation that is
    /// invoked by the <see cref="INpsWebhookProcessor"/> whenever NPS pushes an inbound
    /// message of the given document type. Multiple handlers may be registered per
    /// document type; each distinct implementation is registered once (scoped).
    /// </summary>
    /// <typeparam name="THandler">The handler implementation.</typeparam>
    /// <typeparam name="TDocument">The ISO 20022 document type it handles, e.g. Pacs008Document.</typeparam>
    public static IServiceCollection AddNpsMessageHandler<THandler, TDocument>(this IServiceCollection services)
        where THandler : class, INpsMessageHandler<TDocument>
        where TDocument : class, INpsDocument
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Scoped<INpsMessageHandler<TDocument>, THandler>());
        return services;
    }

    private static IServiceCollection AddNpsIntegrationCore(this IServiceCollection services)
    {
        services.TryAddSingleton<INpsKeyProvider, NpsPemKeyProvider>();
        services.TryAddSingleton<INpsMessageProtector, NpsMessageProtector>();
        services.TryAddSingleton<INpsMessageFactory, NpsMessageFactory>();
        services.TryAddSingleton<INpsInboundMessageProcessor, NpsInboundMessageProcessor>();

        // Scoped so it can resolve scoped INpsMessageHandler<T> implementations.
        services.TryAddScoped<INpsWebhookProcessor, NpsWebhookProcessor>();

        services.AddHttpClient<INpsApiClient, NpsApiClient>(static (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<NpsOptions>>().Value;
            client.Timeout = options.Timeout;
        });

        return services;
    }
}