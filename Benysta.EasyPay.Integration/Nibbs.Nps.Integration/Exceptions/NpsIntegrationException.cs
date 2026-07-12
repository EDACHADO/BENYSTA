using Nibbs.Nps.Integration.Messages.Admi;

namespace Nibbs.Nps.Integration.Exceptions;

/// <summary>Base exception for all NPS integration failures.</summary>
public class NpsIntegrationException : Exception
{
    public NpsIntegrationException(string message) : base(message) { }

    public NpsIntegrationException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Raised when signing, signature validation, encryption or decryption of an
/// ISO 20022 message fails.
/// </summary>
public class NpsSecurityException : NpsIntegrationException
{
    public NpsSecurityException(string message) : base(message) { }

    public NpsSecurityException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Raised when the switch rejects a message at technical/validation level
/// (HTTP 400 carrying an admi.002 Message Reject payload).
/// </summary>
public class NpsMessageRejectedException : NpsIntegrationException
{
    public NpsMessageRejectedException(Admi002Document rejection)
        : base(BuildMessage(rejection))
    {
        Rejection = rejection;
    }

    /// <summary>The decrypted admi.002 rejection returned by the switch.</summary>
    public Admi002Document Rejection { get; }

    /// <summary>Rejection reason code, e.g. "DT01", "AC01".</summary>
    public string ReasonCode => Rejection.MessageReject?.Reason?.RejectingPartyReason;

    /// <summary>MsgId of the message that was rejected.</summary>
    public string OriginalMessageId => Rejection.MessageReject?.RelatedReference?.Reference;

    private static string BuildMessage(Admi002Document rejection)
    {
        var reason = rejection.MessageReject?.Reason;
        return $"NPS rejected message '{rejection.MessageReject?.RelatedReference?.Reference}' " +
               $"with reason {reason?.RejectingPartyReason}: {reason?.ReasonDescription}";
    }
}
