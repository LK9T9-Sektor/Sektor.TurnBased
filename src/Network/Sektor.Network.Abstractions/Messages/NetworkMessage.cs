namespace Sektor.Network.Abstractions.Messages;

/// <summary>
/// Сетевое сообщение лобби. Тип + JSON-полезная нагрузка.
/// </summary>
public sealed class NetworkMessage
{
    /// <summary>Тип сообщения (строковый ключ).</summary>
    public string Type { get; }

    /// <summary>JSON-полезная нагрузка.</summary>
    public string Payload { get; }

    /// <summary>Создаёт сообщение.</summary>
    public NetworkMessage(string type, string payload)
    {
        Type = type;
        Payload = payload;
    }
}
