namespace Integration.BusinessLogics.Nps.Models;

/// <summary>API response detailing an admi.002 validation rejection from the NPS switch.</summary>
/// <param name="ReasonCode">The rejection reason code, e.g. "DT01", "AC01".</param>
/// <param name="OriginalMessageId">The MsgId of the rejected message.</param>
/// <param name="Message">A human-readable description of the rejection.</param>
public sealed record NibbsRejectionResult(string? ReasonCode, string? OriginalMessageId, string Message);
