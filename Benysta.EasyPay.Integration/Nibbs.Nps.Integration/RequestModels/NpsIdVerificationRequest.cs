namespace Nibbs.Nps.Integration.RequestModels;

/// <summary>Input for building an acmt.023 name enquiry.</summary>
public class NpsIdVerificationRequest
{
    public string MessageId { get; set; }

    /// <summary>NPS member id of the institution holding the account.</summary>
    public string AccountAgentId { get; set; } = string.Empty;

    /// <summary>The 10-digit account number to verify.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Optional expected name of the account holder.</summary>
    public string PartyName { get; set; }
}
