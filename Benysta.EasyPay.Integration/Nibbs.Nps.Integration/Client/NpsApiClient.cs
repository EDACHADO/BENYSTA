using System.Text;
using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Exceptions;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Admi;
using Nibbs.Nps.Integration.Messages.Pacs;
using Nibbs.Nps.Integration.Messages.Pain;
using Nibbs.Nps.Integration.Serialization;

namespace Nibbs.Nps.Integration.Client;

public class NpsApiClient : INpsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly INpsMessageProtector _protector;
    private readonly NpsOptions _options;

    public NpsApiClient(HttpClient httpClient, INpsMessageProtector protector, IOptions<NpsOptions> options)
    {
        _httpClient = httpClient;
        _protector = protector;
        _options = options.Value;
    }

    public Task<NpsResponse> SendCreditTransferAsync(Pacs008Document message, CancellationToken cancellationToken = default)
        => SendAsync(message, cancellationToken);

    public Task<NpsResponse> SendPaymentStatusReportAsync(Pacs002Document message, CancellationToken cancellationToken = default)
        => SendAsync(message, cancellationToken);

    public Task<NpsResponse> SendPaymentStatusRequestAsync(Pacs028Document message, CancellationToken cancellationToken = default)
        => SendAsync(message, cancellationToken);

    public Task<NpsResponse> SendIdVerificationRequestAsync(Acmt023Document message, CancellationToken cancellationToken = default)
        => SendAsync(message, cancellationToken);

    public Task<NpsResponse> SendIdVerificationReportAsync(Acmt024Document message, CancellationToken cancellationToken = default)
        => SendAsync(message, cancellationToken);

    public Task<NpsResponse> SendCreditTransferInitiationAsync(Pain001Document message, CancellationToken cancellationToken = default)
        => SendAsync(message, cancellationToken);

    public Task<NpsResponse> SendCustomerPaymentStatusReportAsync(Pain002Document message, CancellationToken cancellationToken = default)
        => SendAsync(message, cancellationToken);

    public Task<NpsResponse> SendAsync<TDocument>(TDocument message, CancellationToken cancellationToken = default)
        where TDocument : INpsDocument
    {
        ArgumentNullException.ThrowIfNull(message);
        return SendRawAsync(message.MessageType, NpsXmlSerializer.Serialize(message), cancellationToken);
    }

    public async Task<NpsResponse> SendRawAsync(NpsMessageType messageType, string plainXml, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainXml);

        var info = NpsMessageTypeInfo.For(messageType);
        var payload = _options.ProtectOutboundMessages ? _protector.Protect(plainXml) : plainXml;

        using var content = new StringContent(payload, Encoding.UTF8, "application/xml");
        using var response = await _httpClient
            .PostAsync(BuildUri(info.EndpointFamily), content, cancellationToken)
            .ConfigureAwait(false);

        return await ReadResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetParticipantsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient
            .GetAsync(BuildUri(NpsEndpoints.GetParticipants), cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new NpsIntegrationException($"Get participants failed with HTTP {(int)response.StatusCode}: {body}");

        return body;
    }

    private Uri BuildUri(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new NpsIntegrationException("NpsOptions.BaseUrl is not configured.");
        return new Uri($"{_options.BaseUrl.TrimEnd('/')}/{relativePath}", UriKind.Absolute);
    }

    private async Task<NpsResponse> ReadResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        string plainBody = null;
        Admi002Document? rejection = null;

        if (!string.IsNullOrWhiteSpace(rawBody) && rawBody.TrimStart().StartsWith('<'))
        {
            plainBody = TryUnprotect(rawBody);
            if (plainBody is not null &&
                NpsXmlSerializer.DetectMessageType(plainBody) == NpsMessageType.Admi002)
            {
                rejection = NpsXmlSerializer.Deserialize<Admi002Document>(plainBody);
            }
        }

        return new NpsResponse
        {
            StatusCode = response.StatusCode,
            IsSuccess = response.IsSuccessStatusCode,
            RawBody = rawBody,
            PlainBody = plainBody,
            Rejection = rejection,
        };
    }

    private string TryUnprotect(string body)
    {
        try
        {
            return _protector.Unprotect(body);
        }
        catch (NpsIntegrationException)
        {
            // Body was not a protected NPS message (e.g. plain error XML); surface the raw body only.
            return null;
        }
    }
}
