# Nibbs.Nps.Integration

Integration library for the NIBSS **National Payment Stack (NPS)** — Nigeria's ISO 20022
instant-payment switch — implemented per the official
[NPS Integration Guide](https://nps-documentation.nibss-plc.com.ng/docs/national-payment-stack-nps/at4heqfolm224-welcome-page) (v1.3).

## What it implements

| Concern | Namespace | Classes |
|---|---|---|
| Switch + gateway settings | `Configuration` | `NpsOptions`, `NpsGatewayOptions` |
| Message metadata, endpoints, ISO codes | `Constants` | `NpsMessageType`, `NpsMessageTypeInfo`, `NpsXmlNamespaces`, `NpsEndpoints`, `TransactionStatus`, `NpsIdType`, `NpsChannelCode`, … |
| Typed ISO 20022 models | `Messages.Pacs` / `.Acmt` / `.Pain` / `.Admi` / `.Common` | `Pacs008Document` (credit transfer), `Pacs002Document` (status report), `Pacs028Document` (status request), `Acmt023Document`/`Acmt024Document` (name enquiry), `Pain001Document`/`Pain002Document` (NIBSS Institution), `Admi002Document` (message reject) |
| Message construction | `Messages` | `INpsMessageFactory` — applies all NPS fixed values (CLRG, RTNS, SLEV, CTAA, 35-char MsgId, ISO-8601 timestamps, 2-dp amounts) |
| Wire-format serialization | `Serialization` | `NpsXmlSerializer` — raw single-line UTF-8 XML, `ns2:Document` root, unqualified children, exactly as the NIBSS samples |
| Signing & encryption | `Cryptography` | `NpsXmlSigner` (enveloped XMLDSIG, RSA-SHA256, C14N), `NpsXmlEncryptor` (AES-256-CBC/GCM payload + RSA-OAEP key wrap), `NpsMessageProtector` (sign→encrypt outbound, decrypt→verify inbound), `INpsKeyProvider` |
| HTTP transport | `Client` | `INpsApiClient` (POST `/nps/pacs`, `/nps/acmt`, `/nps/pain`, `/nps/camt`, GET `/nps/getParticipants`), `INibssInstitutionGatewayClient` (API-Gateway pain.001/pain.008 + token reset) |
| Inbound callbacks | `Inbound` | `INpsInboundMessageProcessor` — decrypts, verifies and types messages NPS pushes to your callback URL |
| Errors | `Exceptions` | `NpsIntegrationException`, `NpsSecurityException`, `NpsMessageRejectedException` (carries the admi.002 reason) |

Message types without a typed model yet (pacs.003, pain.008–pain.014, camt.052/053/060)
can be sent via `INpsApiClient.SendRawAsync(NpsMessageType, plainXml)` — routing,
signing and encryption are still applied.

## Registration

```csharp
builder.Services.AddNpsIntegration(options =>
{
    options.BaseUrl = "https://nps-test.nibss-plc.com.ng:8022/nps";
    options.SourceId = "999058";                       // your NPS member id
    options.InstitutionName = "Your Bank";
    options.PrivateKeyPem = "/secrets/institution.key";  // PEM content or file path
    options.NibssPublicKeyPem = "/secrets/nibss.pub";
});

// Optional: NIBSS Institution request messages via the API Gateway
builder.Services.AddNibssInstitutionGateway(options =>
{
    options.BaseUrl = "https://apitest.nibss-plc.com.ng:1443/nibss-inst";
    options.ClientId = "...";
    options.ClientSecret = "...";
});
```

## Sending a payment (pacs.008)

```csharp
var message = messageFactory.CreateCreditTransfer(new NpsCreditTransferRequest
{
    Amount = 78000m,
    CreditorAgentId = "999057",
    DebtorName = "James",
    DebtorAccountNumber = "0177136558",
    CreditorName = "Musa",
    CreditorAccountNumber = "1029384756",
    Narration = "Invoice 42",
    ChannelCode = NpsChannelCode.MobileApp,
    NameEnquiryMessageId = enquiryMsgId,
    DebtorInfo = new PartyVerificationInfo { AccountDesignation = "1", IdType = NpsIdType.Bvn, IdValue = "2211232344", AccountTier = "1" },
    CreditorInfo = new PartyVerificationInfo { AccountDesignation = "1", IdType = NpsIdType.Bvn, IdValue = "2211232346", AccountTier = "1" },
});

var response = await npsClient.SendCreditTransferAsync(message);
response.EnsureAccepted(); // throws NpsMessageRejectedException on admi.002 rejection
// The business outcome (pacs.002 ACSC/RJCT) arrives on your inbound callback URL.
```

## Name enquiry (acmt.023) and answering inbound messages

```csharp
var enquiry = messageFactory.CreateIdVerificationRequest(new NpsIdVerificationRequest
{
    AccountAgentId = "999012",
    AccountNumber = "1029384756",
});
await npsClient.SendIdVerificationRequestAsync(enquiry);
```

Inbound endpoint (NPS pushes to `https://<your-base-url>/<npsMessageType>`; return HTTP 200 on receipt):

```csharp
app.MapPost("/pacs008", async (HttpRequest request, INpsInboundMessageProcessor processor,
                               INpsMessageFactory factory, INpsApiClient client) =>
{
    using var reader = new StreamReader(request.Body);
    var inbound = processor.Process(await reader.ReadToEndAsync()); // decrypt + verify signature
    var payment = inbound.GetDocument<Pacs008Document>();

    // ... credit the beneficiary, then answer with pacs.002 ACSC/RJCT as a NEW message:
    var report = factory.CreatePaymentStatusReport(new NpsPaymentStatusReportRequest
    {
        OriginalSenderId = payment.CreditTransfer!.GroupHeader!.InstructingAgent!.FinancialInstitutionId!.ClearingSystemMemberId!.MemberId!,
        OriginalMessageId = payment.CreditTransfer.GroupHeader.MessageId!,
        OriginalCreationDateTime = payment.CreditTransfer.GroupHeader.CreationDateTime!,
        OriginalTransactionId = payment.CreditTransfer.Transaction!.PaymentId!.TransactionId,
        OriginalSettlementDate = payment.CreditTransfer.Transaction.InterbankSettlementDate,
        Status = TransactionStatus.AcceptedSettlementCompleted,
    });
    _ = client.SendPaymentStatusReportAsync(report); // fire the response leg
    return Results.Ok();
});
```

## Security model (per the guide)

- **Outbound**: the plaintext document is signed (enveloped XMLDSIG, RSA-SHA256, SHA-256
  digest, inclusive C14N), then only the **content of the business element**
  (`FIToFICstmrCdtTrf`, `IdVrfctnReq`, …) is encrypted — AES-256 session key wrapped with
  the NIBSS RSA public key (RSA-OAEP). The signature stays outside the encrypted block.
- **Inbound**: payload is decrypted with your private key first, then the signature is
  validated with the NIBSS public key (the two must be handled together). NIBSS responses
  using AES-256-GCM are handled transparently.
- Messages are transmitted as **raw, unindented XML strings** — HTTP 400 without a body
  means the switch could not decrypt your message (`NpsResponse.IsDecryptionFailure`);
  HTTP 400 with a body is decrypted into an admi.002 rejection (`NpsResponse.Rejection`).

## Environment notes

- Test endpoints: extranet VPN `https://192.234.10.105:8022/nps`, internet VPN
  `https://nps-test.nibss-plc.com.ng:8022/nps`; traffic goes out via port 8022.
- Static IPs must be whitelisted with NIBSS; your inbound base URL, port and path are
  registered with the onboarding team (one callback URL per environment).
- The gateway token reset contract is institution-specific — override header names via
  `NpsGatewayOptions.ResetHeaders` if your onboarding pack differs.
