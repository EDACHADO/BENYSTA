namespace Nibbs.Nps.Integration.Constants;

/// <summary>
/// Static metadata for each NPS message type: ISO 20022 message name identifier,
/// XML document namespace, business (root payload) element name and the NPS
/// endpoint family the message is posted to.
/// </summary>
public sealed record NpsMessageTypeInfo(
    NpsMessageType Type,
    string MessageNameId,
    string XmlNamespace,
    string BusinessElementName,
    string EndpointFamily)
{
    private static readonly Dictionary<NpsMessageType, NpsMessageTypeInfo> ByType = new()
    {
        [NpsMessageType.Pacs008] = new(NpsMessageType.Pacs008, "pacs.008.001.12", NpsXmlNamespaces.Pacs008, "FIToFICstmrCdtTrf", NpsEndpoints.Pacs),
        [NpsMessageType.Pacs002] = new(NpsMessageType.Pacs002, "pacs.002.001.12", NpsXmlNamespaces.Pacs002, "FIToFIPmtStsRpt", NpsEndpoints.Pacs),
        [NpsMessageType.Pacs003] = new(NpsMessageType.Pacs003, "pacs.003.001.11", NpsXmlNamespaces.Pacs003, "FIToFICstmrDrctDbt", NpsEndpoints.Pacs),
        [NpsMessageType.Pacs028] = new(NpsMessageType.Pacs028, "pacs.028.001.06", NpsXmlNamespaces.Pacs028, "FIToFIPmtStsReq", NpsEndpoints.Pacs),
        [NpsMessageType.Acmt023] = new(NpsMessageType.Acmt023, "acmt.023.001.04", NpsXmlNamespaces.Acmt023, "IdVrfctnReq", NpsEndpoints.Acmt),
        [NpsMessageType.Acmt024] = new(NpsMessageType.Acmt024, "acmt.024.001.04", NpsXmlNamespaces.Acmt024, "IdVrfctnRpt", NpsEndpoints.Acmt),
        [NpsMessageType.Pain001] = new(NpsMessageType.Pain001, "pain.001.001.12", NpsXmlNamespaces.Pain001, "CstmrCdtTrfInitn", NpsEndpoints.Pain),
        [NpsMessageType.Pain002] = new(NpsMessageType.Pain002, "pain.002.001.14", NpsXmlNamespaces.Pain002, "CstmrPmtStsRpt", NpsEndpoints.Pain),
        [NpsMessageType.Pain008] = new(NpsMessageType.Pain008, "pain.008.001.11", NpsXmlNamespaces.Pain008, "CstmrDrctDbtInitn", NpsEndpoints.Pain),
        [NpsMessageType.Pain009] = new(NpsMessageType.Pain009, "pain.009.001.08", NpsXmlNamespaces.Pain009, "MndtInitnReq", NpsEndpoints.Pain),
        [NpsMessageType.Pain010] = new(NpsMessageType.Pain010, "pain.010.001.08", NpsXmlNamespaces.Pain010, "MndtAmdmntReq", NpsEndpoints.Pain),
        [NpsMessageType.Pain011] = new(NpsMessageType.Pain011, "pain.011.001.08", NpsXmlNamespaces.Pain011, "MndtCxlReq", NpsEndpoints.Pain),
        [NpsMessageType.Pain012] = new(NpsMessageType.Pain012, "pain.012.001.08", NpsXmlNamespaces.Pain012, "MndtAccptncRpt", NpsEndpoints.Pain),
        [NpsMessageType.Pain013] = new(NpsMessageType.Pain013, "pain.013.001.11", NpsXmlNamespaces.Pain013, "CdtrPmtActvtnReq", NpsEndpoints.Pain),
        [NpsMessageType.Pain014] = new(NpsMessageType.Pain014, "pain.014.001.11", NpsXmlNamespaces.Pain014, "CdtrPmtActvtnReqStsRpt", NpsEndpoints.Pain),
        [NpsMessageType.Camt052] = new(NpsMessageType.Camt052, "camt.052.001.13", NpsXmlNamespaces.Camt052, "BkToCstmrAcctRpt", NpsEndpoints.Camt),
        [NpsMessageType.Camt053] = new(NpsMessageType.Camt053, "camt.053.001.13", NpsXmlNamespaces.Camt053, "BkToCstmrStmt", NpsEndpoints.Camt),
        [NpsMessageType.Camt060] = new(NpsMessageType.Camt060, "camt.060.001.07", NpsXmlNamespaces.Camt060, "AcctRptgReq", NpsEndpoints.Camt),
        [NpsMessageType.Admi002] = new(NpsMessageType.Admi002, "admi.002.001.01", NpsXmlNamespaces.Admi002, "MsgRjct", NpsEndpoints.Pacs),
    };

    public static NpsMessageTypeInfo For(NpsMessageType type) =>
        ByType.TryGetValue(type, out var info)
            ? info
            : throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown NPS message type.");

    /// <summary>
    /// Resolves the message type from an ISO 20022 document namespace,
    /// e.g. "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.12".
    /// </summary>
    public static NpsMessageType FromXmlNamespace(string xmlNamespace)
    {
        if (string.IsNullOrEmpty(xmlNamespace))
            return NpsMessageType.Unknown;

        foreach (var info in ByType.Values)
        {
            if (string.Equals(info.XmlNamespace, xmlNamespace, StringComparison.OrdinalIgnoreCase))
                return info.Type;
        }

        // Tolerate a different minor version of the same message, e.g. pacs.008.001.13.
        var marker = ExtractMessageFamily(xmlNamespace);
        if (marker is not null)
        {
            foreach (var info in ByType.Values)
            {
                if (info.MessageNameId.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                    return info.Type;
            }
        }

        return NpsMessageType.Unknown;
    }

    private static string ExtractMessageFamily(string xmlNamespace)
    {
        const string prefix = "urn:iso:std:iso:20022:tech:xsd:";
        if (!xmlNamespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        // "pacs.008.001.12" -> "pacs.008."
        var nameId = xmlNamespace[prefix.Length..];
        var parts = nameId.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}." : null;
    }
}
