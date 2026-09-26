namespace Integration.Models.Nps;

/// <summary>
/// Column widths shared by the NPS request/response log tables, sized from the ISO 20022
/// element constraints in the NIBSS NPS integration guide.
/// </summary>
public static class NpsLogColumn
{
    /// <summary>MsgId, InstrId, EndToEndId, TxId, PmtInfId — all fixed at 35 by NPS.</summary>
    public const int MessageId = 35;

    /// <summary>CreDtTm / date elements, stored as the exact wire string.</summary>
    public const int WireDateTime = 35;

    /// <summary>ClrSysMmbId/MmbId. NIBSS member ids are 6 digits; sized for a BIC as well.</summary>
    public const int BankCode = 11;

    /// <summary>
    /// NUBAN is 10 digits. Held wider deliberately: an over-length value from an inbound
    /// message must still be loggable rather than aborting the insert.
    /// </summary>
    public const int AccountNumber = 20;

    /// <summary>Party and account names (ISO Max140Text).</summary>
    public const int PartyName = 140;

    /// <summary>RmtInf/Ustrd (ISO Max140Text).</summary>
    public const int Narration = 140;

    /// <summary>Reason codes, status codes, KYC id values.</summary>
    public const int Code = 35;

    /// <summary>Short coded values: TxSts, GrpSts, StsId, ChannelCode, AccountTier.</summary>
    public const int ShortCode = 10;

    /// <summary>ISO currency code.</summary>
    public const int Currency = 3;

    /// <summary>StsRsnInf/AddtlInf.</summary>
    public const int ReasonInformation = 500;

    /// <summary>Dispatch failure detail (exception messages, truncate on write).</summary>
    public const int ErrorMessage = 1000;

    /// <summary>Enum values persisted as text for legibility in ad-hoc SQL.</summary>
    public const int EnumName = 25;

    /// <summary>Geo-coordinates or location reference.</summary>
    public const int Location = 100;
}
