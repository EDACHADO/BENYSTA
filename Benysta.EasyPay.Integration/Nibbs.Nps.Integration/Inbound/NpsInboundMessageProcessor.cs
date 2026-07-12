using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Admi;
using Nibbs.Nps.Integration.Messages.Pacs;
using Nibbs.Nps.Integration.Messages.Pain;
using Nibbs.Nps.Integration.Serialization;

namespace Nibbs.Nps.Integration.Inbound;

/// <summary>
/// Processes signed/encrypted messages that NPS pushes to your institution's inbound
/// endpoint: decrypts the payload, validates the signature, detects the message type
/// and materializes the typed document. Your endpoint should return HTTP 200
/// immediately after successful processing, per the integration guide.
/// </summary>
public interface INpsInboundMessageProcessor
{
    /// <summary>Decrypts, verifies and parses a raw inbound NPS message body.</summary>
    NpsInboundMessage Process(string rawXml);
}

public class NpsInboundMessageProcessor : INpsInboundMessageProcessor
{
    private readonly INpsMessageProtector _protector;

    public NpsInboundMessageProcessor(INpsMessageProtector protector)
    {
        _protector = protector;
    }

    public NpsInboundMessage Process(string rawXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawXml);

        var plainXml = _protector.Unprotect(rawXml);
        var messageType = NpsXmlSerializer.DetectMessageType(plainXml);

        return new NpsInboundMessage
        {
            MessageType = messageType,
            PlainXml = plainXml,
            Document = Materialize(messageType, plainXml),
        };
    }

    internal static INpsDocument? Materialize(NpsMessageType messageType, string plainXml) => messageType switch
    {
        NpsMessageType.Pacs008 => NpsXmlSerializer.Deserialize<Pacs008Document>(plainXml),
        NpsMessageType.Pacs002 => NpsXmlSerializer.Deserialize<Pacs002Document>(plainXml),
        NpsMessageType.Pacs028 => NpsXmlSerializer.Deserialize<Pacs028Document>(plainXml),
        NpsMessageType.Acmt023 => NpsXmlSerializer.Deserialize<Acmt023Document>(plainXml),
        NpsMessageType.Acmt024 => NpsXmlSerializer.Deserialize<Acmt024Document>(plainXml),
        NpsMessageType.Pain001 => NpsXmlSerializer.Deserialize<Pain001Document>(plainXml),
        NpsMessageType.Pain002 => NpsXmlSerializer.Deserialize<Pain002Document>(plainXml),
        NpsMessageType.Admi002 => NpsXmlSerializer.Deserialize<Admi002Document>(plainXml),
        _ => null,
    };
}
