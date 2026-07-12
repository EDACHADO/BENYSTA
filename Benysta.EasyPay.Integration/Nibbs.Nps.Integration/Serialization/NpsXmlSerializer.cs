using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Exceptions;

namespace Nibbs.Nps.Integration.Serialization;

/// <summary>
/// Serializes and deserializes NPS ISO 20022 documents in the exact wire format
/// mandated by the integration guide: UTF-8, standalone="no", no indentation,
/// the "ns2" prefix bound to the ISO namespace on the Document element,
/// and all child elements unqualified.
/// </summary>
public static class NpsXmlSerializer
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Serializes a typed NPS document to its raw single-line XML string.</summary>
    public static string Serialize<TDocument>(TDocument document) where TDocument : INpsDocument
    {
        ArgumentNullException.ThrowIfNull(document);

        var info = NpsMessageTypeInfo.For(document.MessageType);
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("ns2", info.XmlNamespace);

        var settings = new XmlWriterSettings
        {
            Encoding = Utf8NoBom,
            Indent = false,
            OmitXmlDeclaration = true,
        };

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
        using (var writer = XmlWriter.Create(sb, settings))
        {
            new XmlSerializer(typeof(TDocument)).Serialize(writer, document, namespaces);
        }

        return sb.ToString();
    }

    /// <summary>Loads a typed NPS document into an <see cref="XmlDocument"/> DOM (for signing/encryption).</summary>
    public static XmlDocument SerializeToXmlDocument<TDocument>(TDocument document) where TDocument : INpsDocument
        => LoadXml(Serialize(document));

    /// <summary>Deserializes raw XML into the given NPS document type.</summary>
    public static TDocument Deserialize<TDocument>(string xml) where TDocument : INpsDocument
    {
        ArgumentNullException.ThrowIfNull(xml);
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml));
            var result = new XmlSerializer(typeof(TDocument)).Deserialize(reader)
                         ?? throw new NpsIntegrationException($"XML did not deserialize to {typeof(TDocument).Name}.");
            return (TDocument)result;
        }
        catch (InvalidOperationException ex)
        {
            throw new NpsIntegrationException($"Failed to deserialize XML as {typeof(TDocument).Name}.", ex);
        }
    }

    /// <summary>Detects the NPS message type from a raw XML document's root namespace.</summary>
    public static NpsMessageType DetectMessageType(string xml)
        => NpsMessageTypeInfo.FromXmlNamespace(LoadXml(xml).DocumentElement?.NamespaceURI);

    /// <summary>
    /// Loads XML into a DOM preserving whitespace exactly, as required for
    /// signature computation and validation.
    /// </summary>
    public static XmlDocument LoadXml(string xml)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        return doc;
    }

    /// <summary>Renders a DOM back to its raw string form without adding any formatting.</summary>
    public static string ToXmlString(XmlDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var sb = new StringBuilder();
        if (document.FirstChild is not XmlDeclaration)
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");

        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = false,
            OmitXmlDeclaration = document.FirstChild is not XmlDeclaration,
        });
        document.WriteTo(writer);
        writer.Flush();
        return sb.ToString();
    }
}
