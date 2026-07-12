namespace Nibbs.Nps.Integration.Constants;

/// <summary>
/// ISO 20022 XML document namespaces used on the NPS platform, plus the
/// W3C security namespaces used for message signing and encryption.
/// </summary>
public static class NpsXmlNamespaces
{
    public const string Pacs008 = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.12";
    public const string Pacs002 = "urn:iso:std:iso:20022:tech:xsd:pacs.002.001.12";
    public const string Pacs003 = "urn:iso:std:iso:20022:tech:xsd:pacs.003.001.11";
    public const string Pacs028 = "urn:iso:std:iso:20022:tech:xsd:pacs.028.001.06";
    public const string Acmt023 = "urn:iso:std:iso:20022:tech:xsd:acmt.023.001.04";
    public const string Acmt024 = "urn:iso:std:iso:20022:tech:xsd:acmt.024.001.04";
    public const string Pain001 = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.12";
    public const string Pain002 = "urn:iso:std:iso:20022:tech:xsd:pain.002.001.14";
    public const string Pain008 = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.11";
    public const string Pain009 = "urn:iso:std:iso:20022:tech:xsd:pain.009.001.08";
    public const string Pain010 = "urn:iso:std:iso:20022:tech:xsd:pain.010.001.08";
    public const string Pain011 = "urn:iso:std:iso:20022:tech:xsd:pain.011.001.08";
    public const string Pain012 = "urn:iso:std:iso:20022:tech:xsd:pain.012.001.08";
    public const string Pain013 = "urn:iso:std:iso:20022:tech:xsd:pain.013.001.11";
    public const string Pain014 = "urn:iso:std:iso:20022:tech:xsd:pain.014.001.11";
    public const string Camt052 = "urn:iso:std:iso:20022:tech:xsd:camt.052.001.13";
    public const string Camt053 = "urn:iso:std:iso:20022:tech:xsd:camt.053.001.13";
    public const string Camt060 = "urn:iso:std:iso:20022:tech:xsd:camt.060.001.07";
    public const string Admi002 = "urn:iso:std:iso:20022:tech:xsd:admi.002.001.01";

    public const string XmlDsig = "http://www.w3.org/2000/09/xmldsig#";
    public const string XmlEnc = "http://www.w3.org/2001/04/xmlenc#";
    public const string XmlEnc11 = "http://www.w3.org/2009/xmlenc11#";

    public const string Aes256Cbc = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";
    public const string Aes256Gcm = "http://www.w3.org/2009/xmlenc11#aes256-gcm";
    public const string RsaOaepMgf1p = "http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p";
    public const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
}
