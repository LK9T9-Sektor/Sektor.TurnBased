namespace Sektor.Network.Abstractions.Transport;

/// <summary>
/// Immutable network message. SenderId is stamped by the transport at receive time;
/// the sender does not set it.
/// </summary>
public sealed class TransportMessage
{
    /// <summary>Opaque player id of the sender (set by transport on receive).</summary>
    public string SenderId { get; }

    /// <summary>Message type discriminator (string key, not enum).</summary>
    public string Type { get; }

    /// <summary>Serialized payload (text).</summary>
    public string Payload { get; }

    /// <summary>Creates a new transport message.</summary>
    public TransportMessage(string senderId, string type, string payload)
    {
        SenderId = senderId;
        Type = type;
        Payload = payload;
    }
}
