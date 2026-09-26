namespace Integration.Models.Nps;

/// <summary>
/// Which side originated the exchange being logged.
/// </summary>
public enum NpsLogDirection
{
    /// <summary>We sent the request to NIBSS; the outcome arrives on our callback URL.</summary>
    Outbound = 1,

    /// <summary>NIBSS pushed the request to one of our callback URLs; we answered it.</summary>
    Inbound = 2
}
