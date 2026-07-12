using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Exceptions;
using Nibbs.Nps.Integration.Messages;
using Nibbs.Nps.Integration.Messages.Pain;
using Nibbs.Nps.Integration.Serialization;

namespace Nibbs.Nps.Integration.Client;

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
    /// Calls the token reset endpoint with the onboarding credentials and caches the
    /// returned access token for subsequent requests.
    /// </summary>
    Task<string> ResetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Submits a pain.001 customer credit transfer initiation.</summary>
    Task<NpsResponse> SendCreditTransferInitiationAsync(Pain001Document message, CancellationToken cancellationToken = default);

    /// <summary>Submits a raw plaintext ISO 20022 message to a gateway endpoint (e.g. pain.008).</summary>
    Task<NpsResponse> SendRawAsync(string relativePath, string plainXml, CancellationToken cancellationToken = default);
}

public class NibssInstitutionGatewayClient : INibssInstitutionGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly INpsMessageProtector _protector;
    private readonly NpsGatewayOptions _options;
    private string _accessToken;

    public NibssInstitutionGatewayClient(
        HttpClient httpClient,
        INpsMessageProtector protector,
        IOptions<NpsGatewayOptions> options)
    {
        _httpClient = httpClient;
        _protector = protector;
        _options = options.Value;
    }

    public async Task<string> ResetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(NpsEndpoints.Gateway.Reset));

        // Default NIBSS API Gateway credential headers; override via ResetHeaders when
        // your onboarding pack specifies different names.
        if (_options.ResetHeaders.Count > 0)
        {
            foreach (var (name, value) in _options.ResetHeaders)
                request.Headers.TryAddWithoutValidation(name, value);
        }
        else
        {
            request.Headers.TryAddWithoutValidation("client_id", _options.ClientId);
            request.Headers.TryAddWithoutValidation("client_secret", _options.ClientSecret);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new NpsIntegrationException($"Token reset failed with HTTP {(int)response.StatusCode}: {body}");

        _accessToken = ExtractAccessToken(body)
            ?? throw new NpsIntegrationException("Token reset succeeded but no access token was found in the response.");
        return _accessToken;
    }

    public Task<NpsResponse> SendCreditTransferInitiationAsync(Pain001Document message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return SendRawAsync(NpsEndpoints.Gateway.Pain001, NpsXmlSerializer.Serialize(message), cancellationToken);
    }

    public async Task<NpsResponse> SendRawAsync(string relativePath, string plainXml, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(plainXml);

        if (_accessToken is null)
            await ResetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        var payload = _protector.Protect(plainXml);

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(relativePath))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/xml"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new NpsResponse
        {
            StatusCode = response.StatusCode,
            IsSuccess = response.IsSuccessStatusCode,
            RawBody = rawBody,
        };
    }

    private Uri BuildUri(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new NpsIntegrationException("NpsGatewayOptions.BaseUrl is not configured.");
        return new Uri($"{_options.BaseUrl.TrimEnd('/')}/{relativePath}", UriKind.Absolute);
    }

    private static string ExtractAccessToken(string responseBody)
    {
        try
        {
            using var json = JsonDocument.Parse(responseBody);
            return FindToken(json.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }

        static string FindToken(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String &&
                    (property.NameEquals("access_token") || property.NameEquals("accessToken") ||
                     property.NameEquals("token")))
                    return property.Value.GetString();

                if (property.Value.ValueKind == JsonValueKind.Object &&
                    FindToken(property.Value) is { } nested)
                    return nested;
            }

            return null;
        }
    }
}
