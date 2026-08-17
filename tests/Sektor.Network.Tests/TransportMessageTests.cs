using Sektor.Network.Abstractions.Transport;
using Xunit;

namespace Sektor.Network.Tests;

public class TransportMessageTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var message = new TransportMessage("sender1", "test_type", "test_payload");

        Assert.Equal("sender1", message.SenderId);
        Assert.Equal("test_type", message.Type);
        Assert.Equal("test_payload", message.Payload);
    }

    [Fact]
    public void Constructor_EmptyStrings_AreValid()
    {
        var message = new TransportMessage(string.Empty, string.Empty, string.Empty);

        Assert.Equal(string.Empty, message.SenderId);
        Assert.Equal(string.Empty, message.Type);
        Assert.Equal(string.Empty, message.Payload);
    }
}
