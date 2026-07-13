using Nibbs.Nps.Integration.Messages.Common;

namespace Integration.BusinessLogics.Nps.Abstractions;

/// <summary>
/// Looks up an account held at this institution to answer an inbound acmt.023
/// name enquiry. Implement this against the core banking system and register it
/// via <c>services.AddNpsIdentificationVerificationFlow&lt;TImplementation&gt;()</c>.
/// </summary>
public interface IAccountVerificationService
{
    /// <summary>
    /// Verifies an account number (and optionally the name the requester supplied)
    /// and returns the resolved account details.
    /// </summary>
    /// <param name="accountNumber">The 10-digit account number (NUBAN) to verify.</param>
    /// <param name="suppliedName">The party name supplied by the requester, when present.</param>
    /// <param name="cancellationToken">Aborts the lookup.</param>
    Task<AccountVerificationResult> VerifyAccountAsync(
        string accountNumber,
        string? suppliedName,
        CancellationToken cancellationToken);
}

/// <summary>Outcome of an account verification lookup.</summary>
/// <param name="Verified">Whether the account exists and is enquirable (acmt.024 Vrfctn).</param>
/// <param name="AccountName">The resolved name on the account; required when verified.</param>
/// <param name="AccountHolderInfo">KYC block (AccountDesignation, IdType, IdValue, AccountTier); required when verified per the guide.</param>
/// <param name="RiskRating">Optional risk rating shared in the supplementary block.</param>
public sealed record AccountVerificationResult(
    bool Verified,
    string? AccountName = null,
    PartyVerificationInfo? AccountHolderInfo = null,
    string? RiskRating = null);
