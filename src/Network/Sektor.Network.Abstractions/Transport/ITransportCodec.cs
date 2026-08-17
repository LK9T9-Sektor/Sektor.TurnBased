namespace Sektor.Network.Abstractions.Transport;

/// <summary>
/// Serializes and deserializes transport messages for the wire format.
/// The transport stamps SenderId on deserialize; the codec does not include it.
/// </summary>
public interface ITransportCodec
{
    /// <summary>Serializes a message to wire text.</summary>
    string Serialize(TransportMessage message);

    /// <summary>Deserializes wire text to a message (SenderId left empty; transport fills it).</summary>
    TransportMessage Deserialize(string text);
}
