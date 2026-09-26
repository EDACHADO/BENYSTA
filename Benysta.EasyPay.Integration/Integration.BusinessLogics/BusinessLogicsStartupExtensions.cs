using Integration.BusinessLogics.Nps.Abstractions;
using Integration.BusinessLogics.Nps.Handlers;
using Integration.Infrastructures.Abstractions;
using Integration.Infrastructures.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nibbs.Nps.Integration;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Pacs;

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

        services.AddScoped<IDateTimeService, DateTimeService>();

        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
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

        // Registered first, deliberately. Handlers run in registration order, and the
        // acmt.024 that InboundIdVerificationRequestHandler sends is recorded against the
        // row the logging handler creates — so the log must exist before the reply is built.
        // TryAddEnumerable makes this a no-op if the caller already registered it, which
        // keeps the ordering correct however the startup extensions are sequenced.
        services.AddNpsNameEnquiryLogging();

        services.AddNpsMessageHandler<InboundIdVerificationRequestHandler, Acmt023Document>();
        return services;
    }

    /// <summary>
    /// Enables request/response logging for the name enquiry flow (acmt.023 / acmt.024) in
    /// both directions:
    /// <list type="bullet">
    /// <item>Outbound acmt.023 is logged before dispatch — one row per account enquired
    /// about — and updated with the switch's answer.</item>
    /// <item>The inbound acmt.024 answering it is applied by
    /// <see cref="InboundIdVerificationReportHandler"/>, filling in the resolved account
    /// name and KYC details.</item>
    /// <item>Inbound acmt.023 pushed by NIBSS is recorded, and the acmt.024 we send back
    /// completes that row.</item>
    /// </list>
    /// Requires AddDALApplicationDependencies(), AddIntegrationBusinessLogics() and
    /// AddNpsIntegration(). Called automatically by
    /// <see cref="AddNpsIdentificationVerificationFlow{TAccountVerificationService}"/>.
    /// </summary>
    public static IServiceCollection AddNpsNameEnquiryLogging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddNpsMessageHandler<InboundIdVerificationLogHandler, Acmt023Document>();
        services.AddNpsMessageHandler<InboundIdVerificationReportHandler, Acmt024Document>();
        return services;
    }

    /// <summary>
    /// Enables request/response logging for the single credit transfer flow (pacs.008 /
    /// pacs.002) in both directions, covering NPS certification Phases 1 to 3:
    /// <list type="number">
    /// <item>Outbound pacs.008 is logged before dispatch and updated with the switch's answer.</item>
    /// <item>Inbound pacs.008 pushed by NIBSS is recorded, which is what Phase 2 asks you to observe.</item>
    /// <item>The pacs.002 we send back completes that inbound row.</item>
    /// </list>
    /// The inbound pacs.002 that settles an outbound payment is applied by
    /// <see cref="InboundPaymentStatusReportHandler"/>. Requires AddDALApplicationDependencies(),
    /// AddIntegrationBusinessLogics() and AddNpsIntegration().
    /// </summary>
    public static IServiceCollection AddNpsSingleTransferLogging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddNpsMessageHandler<InboundCreditTransferLogHandler, Pacs008Document>();
        services.AddNpsMessageHandler<InboundPaymentStatusReportHandler, Pacs002Document>();
        return services;
    }
}
