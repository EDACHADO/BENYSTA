namespace Nibbs.Nps.Integration.Constants;

/// <summary>
/// Relative endpoint paths on the NPS switch (base URL: https://&lt;nps-host&gt;:8022/nps)
/// and on the NIBSS Institution API Gateway.
/// Per the integration guide, all messages of a family are posted to the family endpoint,
/// e.g. pacs.008/pacs.002/pacs.028 are all posted to "/nps/pacs".
/// </summary>
public static class NpsEndpoints
{
    /// <summary>Endpoint for pacs.* messages (credit transfer, status report/request, direct debit).</summary>
    public const string Pacs = "pacs";

    /// <summary>Endpoint for acmt.* messages (identification verification / name enquiry).</summary>
    public const string Acmt = "acmt";

    /// <summary>Endpoint for pain.* messages (payment initiation, mandates, request to pay).</summary>
    public const string Pain = "pain";

    /// <summary>Endpoint for camt.* messages (account reporting).</summary>
    public const string Camt = "camt";

    /// <summary>Endpoint returning the list of NPS participants and their statuses.</summary>
    public const string GetParticipants = "getParticipants";

    /// <summary>Gateway (NIBSS Institution) endpoints.</summary>
    public static class Gateway
    {
        public const string Pain001 = "pain/001";
        public const string Pain008 = "pain/008";
        public const string Pain002 = "pain002";
        public const string Acmt023 = "acmt/023";
        public const string Acmt024 = "acmt/024";
        public const string Participants = "participants";
        public const string Reset = "reset";
    }
}
