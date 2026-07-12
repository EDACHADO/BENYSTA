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

    /// <summary>Gateway client id issued during onboarding (used by the token reset endpoint).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gateway client secret / API key issued during onboarding.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Extra headers to send on the token reset request, for deployments where the
    /// gateway expects credentials under specific header names.
    /// </summary>
    public Dictionary<string, string> ResetHeaders { get; set; } = new();

    /// <summary>HTTP timeout for gateway calls.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
}
