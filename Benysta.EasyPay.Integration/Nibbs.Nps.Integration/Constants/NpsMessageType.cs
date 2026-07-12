namespace Nibbs.Nps.Integration.Constants;

/// <summary>
/// ISO 20022 message types supported by the NIBSS National Payment Stack (NPS).
/// </summary>
public enum NpsMessageType
{
    Unknown = 0,

    /// <summary>pacs.008 — FI to FI Customer Credit Transfer (payment request).</summary>
    Pacs008,

    /// <summary>pacs.002 — FI to FI Payment Status Report (payment response ACSC/RJCT).</summary>
    Pacs002,

    /// <summary>pacs.003 — FI to FI Customer Direct Debit.</summary>
    Pacs003,

    /// <summary>pacs.028 — FI to FI Payment Status Request (transaction status query).</summary>
    Pacs028,

    /// <summary>acmt.023 — Identification Verification Request (name enquiry).</summary>
    Acmt023,

    /// <summary>acmt.024 — Identification Verification Report (name enquiry response).</summary>
    Acmt024,

    /// <summary>pain.001 — Customer Credit Transfer Initiation.</summary>
    Pain001,

    /// <summary>pain.002 — Customer Payment Status Report.</summary>
    Pain002,

    /// <summary>pain.008 — Customer Direct Debit Initiation (collection instruction).</summary>
    Pain008,

    /// <summary>pain.009 — Mandate Initiation Request.</summary>
    Pain009,

    /// <summary>pain.010 — Mandate Amendment Request.</summary>
    Pain010,

    /// <summary>pain.011 — Mandate Cancellation Request.</summary>
    Pain011,

    /// <summary>pain.012 — Mandate Acceptance Report.</summary>
    Pain012,

    /// <summary>pain.013 — Creditor Payment Activation Request (Request to Pay).</summary>
    Pain013,

    /// <summary>pain.014 — Creditor Payment Activation Request Status Report.</summary>
    Pain014,

    /// <summary>camt.052 — Bank to Customer Account Report (intraday).</summary>
    Camt052,

    /// <summary>camt.053 — Bank to Customer Statement (end of day).</summary>
    Camt053,

    /// <summary>camt.060 — Account Reporting Request.</summary>
    Camt060,

    /// <summary>admi.002 — Message Reject (technical/validation rejection from the switch).</summary>
    Admi002,
}
