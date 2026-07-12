using Nibbs.Nps.Integration.Client;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Pacs;
using Nibbs.Nps.Integration.Messages.Pain;

namespace Nibbs.Nps.Integration.Abstractions;

/// <summary>
/// Client for the NPS switch (https://&lt;nps-host&gt;:8022/nps). Serializes, signs,
/// encrypts and posts ISO 20022 messages, and decrypts/parses switch responses.
/// </summary>
public interface INpsApiClient
{
    /// <summary>Sends a pacs.008 FI-to-FI customer credit transfer (payment request).</summary>
    Task<NpsResponse> SendCreditTransferAsync(Pacs008Document message, CancellationToken cancellationToken = default);

    /// <summary>Sends a pacs.002 payment status report (ACSC/RJCT response to an inbound payment).</summary>
    Task<NpsResponse> SendPaymentStatusReportAsync(Pacs002Document message, CancellationToken cancellationToken = default);

    /// <summary>Sends a pacs.028 payment status request (transaction status query).</summary>
    Task<NpsResponse> SendPaymentStatusRequestAsync(Pacs028Document message, CancellationToken cancellationToken = default);

    /// <summary>Sends an acmt.023 identification verification request (name enquiry).</summary>
    Task<NpsResponse> SendIdVerificationRequestAsync(Acmt023Document message, CancellationToken cancellationToken = default);

    /// <summary>Sends an acmt.024 identification verification report (name enquiry response).</summary>
    Task<NpsResponse> SendIdVerificationReportAsync(Acmt024Document message, CancellationToken cancellationToken = default);

    /// <summary>Sends a pain.001 customer credit transfer initiation to the NIBSS Institution via NPS.</summary>
    Task<NpsResponse> SendCreditTransferInitiationAsync(Pain001Document message, CancellationToken cancellationToken = default);

    /// <summary>Sends a pain.002 customer payment status report.</summary>
    Task<NpsResponse> SendCustomerPaymentStatusReportAsync(Pain002Document message, CancellationToken cancellationToken = default);

    /// <summary>Sends any typed NPS document to its family endpoint.</summary>
    Task<NpsResponse> SendAsync<TDocument>(TDocument message, CancellationToken cancellationToken = default)
        where TDocument : INpsDocument;

    /// <summary>
    /// Sends a raw plaintext ISO 20022 XML message of the given type (signed and encrypted
    /// by the client before transmission). Use for message types without a typed model
    /// (pacs.003, pain.008–pain.014, camt.052/053/060).
    /// </summary>
    Task<NpsResponse> SendRawAsync(NpsMessageType messageType, string plainXml, CancellationToken cancellationToken = default);

    /// <summary>Gets the list of NPS participants with their active/inactive statuses.</summary>
    Task<string> GetParticipantsAsync(CancellationToken cancellationToken = default);
}
