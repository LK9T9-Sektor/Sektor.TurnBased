using System.Text.Json;
using Sektor.Network.Abstractions.Transport;

namespace Sektor.Network.Steam;

/// <summary>
/// JSON transport codec using System.Text.Json. Wire format: {"type":"...","payload":"..."}.
/// SenderId is not serialized — the transport stamps it at receive time.
/// </summary>
public sealed class JsonTransportCodec : ITransportCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    public string Serialize(TransportMessage message)
    {
        var dto = new MessageDto { Type = message.Type, Payload = message.Payload };
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    /// <inheritdoc />
    public TransportMessage Deserialize(string text)
    {
        var dto = JsonSerializer.Deserialize<MessageDto>(text, JsonOptions);
        return new TransportMessage(string.Empty, dto?.Type ?? string.Empty, dto?.Payload ?? string.Empty);
    }

    private sealed class MessageDto
    {
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
