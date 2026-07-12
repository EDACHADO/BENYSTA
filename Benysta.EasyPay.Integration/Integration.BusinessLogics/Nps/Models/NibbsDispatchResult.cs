namespace Integration.BusinessLogics.Nps.Models;

/// <summary>API response for a message successfully handed to the NPS switch.</summary>
/// <param name="MessageId">The generated ISO 20022 message identifier (GrpHdr/MsgId); correlate webhook callbacks with this.</param>
/// <param name="NibssStatusCode">The HTTP status code the NPS switch returned.</param>
/// <param name="Accepted">Whether the switch acknowledged receipt (HTTP 2xx). This is not the business outcome.</param>
public sealed record NibbsDispatchResult(string MessageId, int NibssStatusCode, bool Accepted);
