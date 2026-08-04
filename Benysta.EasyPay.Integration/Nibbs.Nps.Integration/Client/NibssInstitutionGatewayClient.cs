using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Exceptions;
using Nibbs.Nps.Integration.Messages.Pain;
using Nibbs.Nps.Integration.Serialization;

namespace Nibbs.Nps.Integration.Client;
public class NibssInstitutionGatewayClient : INibssInstitutionGatewayClient
{
    private const string GrantType = "client_credentials";

    /// <summary>Refresh this many seconds before the advertised expiry so a token never dies mid-flight.</summary>
    private const int ExpirySafetyMarginSeconds = 60;

    /// <summary>The client secret is invalid (per the reset endpoint's 401 contract).</summary>
    private const long ErrorCodeInvalidClientSecret = 7000215;

    /// <summary>The client secret has expired and must be regenerated (per the reset endpoint's 401 contract).</summary>
    private const long ErrorCodeExpiredClientSecret = 7000222;

    private readonly HttpClient _httpClient;
    private readonly INpsMessageProtector _protector;
    private readonly NpsGatewayOptions _options;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

    public NibssInstitutionGatewayClient(
        HttpClient httpClient,
        INpsMessageProtector protector,
        IOptions<NpsGatewayOptions> options)
    {
        _httpClient = httpClient;
        _protector = protector;
        _options = options.Value;
    }

    public async Task<NpsGatewayTokenResponse> ResetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(NpsEndpoints.Gateway.Reset));

            // The apiKey issued during onboarding authenticates the reset call itself;
            // it is only used on this endpoint.
            if (!string.IsNullOrEmpty(_options.ApiKey))
                request.Headers.TryAddWithoutValidation("apiKey", _options.ApiKey);
            foreach (var (name, value) in _options.ResetHeaders)
                request.Headers.TryAddWithoutValidation(name, value);

            var scope = string.IsNullOrEmpty(_options.Scope)
                ? $"{_options.ClientId}/.default"
                : _options.Scope;

            var body = JsonSerializer.Serialize(new
            {
                client_id = _options.ClientId,
                scope,
                client_secret = _options.ClientSecret,
                grant_type = GrantType,
            });
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new NpsIntegrationException(BuildResetFailureMessage((int)response.StatusCode, responseBody));

            NpsGatewayTokenResponse token;
            try
            {
                token = JsonSerializer.Deserialize<NpsGatewayTokenResponse>(responseBody);
            }
            catch (JsonException ex)
            {
                throw new NpsIntegrationException("Token reset succeeded but the response body is not valid JSON.", ex);
            }

            if (string.IsNullOrEmpty(token?.AccessToken))
                throw new NpsIntegrationException("Token reset succeeded but no access token was found in the response.");

            _accessToken = token.AccessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(
                Math.Max(token.ExpiresIn - ExpirySafetyMarginSeconds, 0));
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
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

        var accessToken = await GetOrRefreshAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        var payload = _protector.Protect(plainXml);

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(relativePath))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/xml"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new NpsResponse
        {
            StatusCode = response.StatusCode,
            IsSuccess = response.IsSuccessStatusCode,
            RawBody = rawBody,
        };
    }

    /// <summary>Returns the cached access token, resetting it first when absent or (nearly) expired.</summary>
    private async Task<string> GetOrRefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is null || DateTimeOffset.UtcNow >= _accessTokenExpiresAt)
            await ResetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        return _accessToken;
    }

    private Uri BuildUri(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new NpsIntegrationException("NpsGatewayOptions.BaseUrl is not configured.");
        return new Uri($"{_options.BaseUrl.TrimEnd('/')}/{relativePath}", UriKind.Absolute);
    }

    /// <summary>
    /// Builds the failure message from the reset endpoint's error contract
    /// ({ timestamp, error_codes, error_description, error }), including remediation
    /// hints for the documented client-secret error codes.
    /// </summary>
    private static string BuildResetFailureMessage(int statusCode, string responseBody)
    {
        var message = $"Token reset failed with HTTP {statusCode}: {responseBody}";
        try
        {
            using var json = JsonDocument.Parse(responseBody);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
                return message;

            var codes = new List<long>();
            if (json.RootElement.TryGetProperty("error_codes", out var codesElement) &&
                codesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var code in codesElement.EnumerateArray())
                {
                    if (code.TryGetInt64(out var value))
                        codes.Add(value);
                }
            }

            if (json.RootElement.TryGetProperty("error_description", out var description) &&
                description.ValueKind == JsonValueKind.String)
            {
                message = $"Token reset failed with HTTP {statusCode}: {description.GetString()}" +
                          (codes.Count > 0 ? $" (error codes: {string.Join(", ", codes)})" : string.Empty);
            }

            if (codes.Contains(ErrorCodeInvalidClientSecret))
                message += " The client secret is invalid.";
            if (codes.Contains(ErrorCodeExpiredClientSecret))
                message += " The client secret has expired — refresh it via the NIBSS Client Secret Refresh / " +
                           "Secret Generator endpoint before calling reset again.";
        }
        catch (JsonException)
        {
            // Not JSON — fall back to the raw body.
        }

        return message;
    }
}
