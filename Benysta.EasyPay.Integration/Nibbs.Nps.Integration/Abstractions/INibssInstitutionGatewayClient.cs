using Nibbs.Nps.Integration.Client;
using Nibbs.Nps.Integration.Messages.Pain;

namespace Nibbs.Nps.Integration.Abstractions;

/// <summary>
/// Client for the "NIBSS Institution" request-message service exposed through the
/// NIBSS API Gateway (e.g. https://apitest.nibss-plc.com.ng:1443/nibss-inst).
/// Payment instructions are submitted as pain.001, collections as pain.008;
/// outcomes are delivered asynchronously as pain.002. All calls carry a Bearer
/// access token obtained from the token reset endpoint.
/// </summary>
public interface INibssInstitutionGatewayClient
{
    /// <summary>
    /// Calls the token reset endpoint (POST /reset) with the onboarding credentials —
    /// apiKey in the request header; client_id, scope, client_secret and grant_type in
    /// the JSON body — and caches the returned access token, together with its expiry,
    /// for subsequent requests.
    /// </summary>
    Task<NpsGatewayTokenResponse> ResetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Submits a pain.001 customer credit transfer initiation.</summary>
    Task<NpsResponse> SendCreditTransferInitiationAsync(Pain001Document message, CancellationToken cancellationToken = default);

    /// <summary>Submits a raw plaintext ISO 20022 message to a gateway endpoint (e.g. pain.008).</summary>
    Task<NpsResponse> SendRawAsync(string relativePath, string plainXml, CancellationToken cancellationToken = default);
}
