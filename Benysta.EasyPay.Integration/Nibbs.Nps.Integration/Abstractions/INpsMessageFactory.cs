using Nibbs.Nps.Integration.Messages;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Pacs;

namespace Nibbs.Nps.Integration.Abstractions;

/// <summary>
/// Builds NPS messages pre-populated with the fixed values, identifiers and formats
/// mandated by the integration guide (35-character MsgId, CLRG settlement, RTNS
/// clearing channel, SLEV charge bearer, ISO-8601 timestamps, 2-dp amounts).
/// </summary>
public interface INpsMessageFactory
{
    /// <summary>Creates a single pacs.008 credit transfer to another NPS participant.</summary>
    Pacs008Document CreateCreditTransfer(NpsCreditTransferRequest request);

    /// <summary>Creates an acmt.023 name enquiry for an account at another participant.</summary>
    Acmt023Document CreateIdVerificationRequest(NpsIdVerificationRequest request);

    /// <summary>Creates a pacs.002 status report answering an inbound pacs.008.</summary>
    Pacs002Document CreatePaymentStatusReport(NpsPaymentStatusReportRequest request);

    /// <summary>Creates a pacs.028 status request for a previously sent pacs.008.</summary>
    Pacs028Document CreatePaymentStatusRequest(NpsPaymentStatusQuery request);
}
