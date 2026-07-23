using System.Text.Json.Serialization;

namespace Nibbs.Nps.Integration.Client;

/// <summary>
/// Success response of the NIBSS API Gateway token reset endpoint (POST /reset),
/// per the NPS integration guide.
/// </summary>
public sealed class NpsGatewayTokenResponse
{
    /// <summary>The token type, e.g. "Bearer".</summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; }

    /// <summary>The time the token expires, in seconds.</summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    /// <summary>The extended lifetime of the access token, in seconds.</summary>
    [JsonPropertyName("ext_expires_in")]
    public int ExtExpiresIn { get; init; }

    /// <summary>The access token to pass in the Authorization header of subsequent requests.</summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; }
}
