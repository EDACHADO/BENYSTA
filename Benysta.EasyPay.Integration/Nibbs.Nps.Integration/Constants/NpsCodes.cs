namespace Nibbs.Nps.Integration.Constants;

/// <summary>
/// ISO 20022 transaction/group status codes used in pacs.002 / pain.002 / pain.012 / pain.014.
/// </summary>
public static class TransactionStatus
{
    /// <summary>Accepted Settlement Completed — payment approved and settled.</summary>
    public const string AcceptedSettlementCompleted = "ACSC";

    /// <summary>Accepted Settlement In Process.</summary>
    public const string AcceptedSettlementInProcess = "ACSP";

    /// <summary>Accepted Customer Profile — e.g. mandate/RTP responses (pain.012/pain.014).</summary>
    public const string AcceptedCustomerProfile = "ACCP";

    /// <summary>Accepted Technical Validation.</summary>
    public const string AcceptedTechnicalValidation = "ACTC";

    /// <summary>Rejected.</summary>
    public const string Rejected = "RJCT";

    /// <summary>Pending.</summary>
    public const string Pending = "PDNG";
}

/// <summary>
/// Status identification values used in pacs.002 TxInfAndSts/StsId,
/// indicating whether the reporting bank authorized the payment.
/// </summary>
public static class StatusId
{
    public const string Authorized = "AUTH";
    public const string NotAuthorized = "NAUTH";
}

/// <summary>Settlement method — NPS settles via clearing.</summary>
public static class SettlementMethod
{
    public const string Clearing = "CLRG";
}

/// <summary>Clearing channel codes.</summary>
public static class ClearingChannel
{
    /// <summary>Real Time Net Settlement system — fixed value on NPS.</summary>
    public const string RealTimeNetSettlement = "RTNS";
}

/// <summary>Charge bearer codes.</summary>
public static class ChargeBearer
{
    /// <summary>Following service level — fixed value on NPS pacs.008.</summary>
    public const string FollowingServiceLevel = "SLEV";

    public const string Creditor = "CRED";
    public const string Debtor = "DEBT";
}

/// <summary>Local instrument proprietary codes identifying the business process.</summary>
public static class LocalInstrument
{
    /// <summary>Credit transfer account-to-account (default per the NPS samples).</summary>
    public const string CreditTransferAccountToAccount = "CTAA";
}

/// <summary>
/// Identification types accepted in the NPS supplementary data
/// (Debtor/Creditor Info IdType). Per the guide: if NIN, account tier must be 1;
/// JTBTIN / FIRSTIN / RC Number require a corporate account designation.
/// </summary>
public static class NpsIdType
{
    public const string Bvn = "BVN";
    public const string Nin = "NIN";
    public const string JtbTin = "JTBTIN";
    public const string FirsTin = "FIRSTIN";
    public const string RcNumber = "RC Number";
}

/// <summary>Channel codes for the supplementary TransactionInfo/ChannelCode field.</summary>
public static class NpsChannelCode
{
    public const string BankTeller = "1";
    public const string InternetBanking = "2";
    public const string MobileApp = "3";
    public const string Pos = "4";
    public const string Atm = "5";
}

/// <summary>Account designation values for supplementary data.</summary>
public static class AccountDesignation
{
    public const string Individual = "1";
    public const string Corporate = "2";
}
