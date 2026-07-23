namespace Nibbs.Nps.Integration.Configuration;

/// <summary>
/// Options for integrating with the "NIBSS Institution" request-message service
/// through the NIBSS API Gateway (pain.001 / pain.008 / pain.002, token reset).
/// </summary>
public class NpsGatewayOptions
{
    public const string SectionName = "NpsGateway";

    /// <summary>
    /// Base URL of the NIBSS Institution service on the API Gateway,
    /// e.g. "https://apitest.nibss-plc.com.ng:1443/nibss-inst".
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gateway client id assigned during the credential request (sent in the token
    /// reset request body as "client_id").
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gateway client secret assigned during the credential request (sent in the token
    /// reset request body as "client_secret").
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The apiKey assigned during the credential request. Passed in the request header
    /// of the token reset call only, per the integration guide.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// OAuth scope sent in the token reset request body. Per the guide this is
    /// "&lt;client_id&gt;/.default"; leave empty to derive it from <see cref="ClientId"/>.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Additional headers to send on the token reset request, for deployments where the
    /// gateway expects extra or differently named credential headers.
    /// </summary>
    public Dictionary<string, string> ResetHeaders { get; set; } = new();

    /// <summary>HTTP timeout for gateway calls.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
}
