using Integration.BusinessLogics.Nps.Abstractions;

namespace Integration.WebApi.Services;

/// <summary>
/// TODO: replace with a lookup against the core banking system before go-live.
/// Placeholder that answers every inbound acmt.023 name enquiry as not verified
/// (acmt.024 Vrfctn=false), so no account is ever falsely confirmed.
/// </summary>
public sealed class PlaceholderAccountVerificationService(ILogger<PlaceholderAccountVerificationService> logger)
    : IAccountVerificationService
{
    public Task<AccountVerificationResult> VerifyAccountAsync(
        string accountNumber,
        string? suppliedName,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "PlaceholderAccountVerificationService: answering enquiry for account {AccountNumber} as NOT verified. " +
            "Replace this service with a core-banking lookup.",
            accountNumber);

        return Task.FromResult(new AccountVerificationResult(Verified: false));
    }
}
