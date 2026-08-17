using Sektor.Network.Abstractions.Transport;
using Sektor.Network.Steam;
using Xunit;

namespace Sektor.Network.Tests;

public class JsonTransportCodecTests
{
    private readonly JsonTransportCodec _codec = new();

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var message = new TransportMessage("sender1", "test_type", "test_payload");
        string json = _codec.Serialize(message);

        Assert.Contains("\"type\":\"test_type\"", json);
        Assert.Contains("\"payload\":\"test_payload\"", json);
        Assert.DoesNotContain("senderId", json);
    }

    [Fact]
    public void Deserialize_ParsesTypeAndPayload()
    {
        string json = "{\"type\":\"test_type\",\"payload\":\"test_payload\"}";
        TransportMessage message = _codec.Deserialize(json);

        Assert.Equal("test_type", message.Type);
        Assert.Equal("test_payload", message.Payload);
        Assert.Equal(string.Empty, message.SenderId);
    }

    [Fact]
    public void RoundTrip_PreservesTypeAndPayload()
    {
        var original = new TransportMessage("sender1", "my_type", "my_payload");
        string json = _codec.Serialize(original);
        TransportMessage decoded = _codec.Deserialize(json);

        Assert.Equal(original.Type, decoded.Type);
        Assert.Equal(original.Payload, decoded.Payload);
    }

    [Fact]
    public void Serialize_EscapesSpecialCharacters()
    {
        var message = new TransportMessage("sender1", "type", "line1\nline2\ttab\"quote");
        string json = _codec.Serialize(message);
        TransportMessage decoded = _codec.Deserialize(json);

        Assert.Equal("line1\nline2\ttab\"quote", decoded.Payload);
    }

    [Fact]
    public void Deserialize_EmptyJson_ReturnsEmptyMessage()
    {
        string json = "{}";
        TransportMessage message = _codec.Deserialize(json);

        Assert.Equal(string.Empty, message.Type);
        Assert.Equal(string.Empty, message.Payload);
    }
}
