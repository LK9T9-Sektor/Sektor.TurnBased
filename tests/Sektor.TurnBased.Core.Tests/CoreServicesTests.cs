using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Xunit;

namespace Sektor.TurnBased.Core.Tests;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError_AndIsSuccess()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_HasError_AndIsFailure()
    {
        var result = Result.Failure("boom");
        Assert.True(result.IsFailure);
        Assert.Equal("boom", result.Error);
    }

    [Fact]
    public void OfT_Success_ExposesValue()
    {
        var result = Result<int>.Success(42);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void OfT_Failure_HasNullValue()
    {
        var result = Result<int>.Failure("no");
        Assert.True(result.IsFailure);
        Assert.False(result.TryGetValue(out _));
    }
}

public class DeterministicRngTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var rng1 = new DeterministicRng(1234);
        var rng2 = new DeterministicRng(1234);

        for (var i = 0; i < 100; i++)
            Assert.Equal(rng1.Next(0, 1000), rng2.Next(0, 1000));
    }

    [Fact]
    public void Next_RespectsBounds()
    {
        var rng = new DeterministicRng(1);
        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(5, 10);
            Assert.InRange(value, 5, 9);
        }
    }
}

public class GameEventBusTests
{
    private sealed class DamageEvent
    {
        public int Amount { get; set; }
    }

    [Fact]
    public void Raise_AppliesBase()
    {
        var bus = new GameEventBus();
        var applied = 0;

        var result = bus.Raise(new DamageEvent { Amount = 10 }, e => applied = e.Amount);

        Assert.True(result);
        Assert.Equal(10, applied);
    }

    [Fact]
    public void Before_CanCancel_Event()
    {
        var bus = new GameEventBus();
        bus.SubscribeBefore<DamageEvent>(ctx => ctx.IsCancelled = true);
        var applied = false;

        var result = bus.Raise(new DamageEvent { Amount = 10 }, _ => applied = true);

        Assert.False(result);
        Assert.False(applied);
    }

    [Fact]
    public void After_RunsAfterBase()
    {
        var bus = new GameEventBus();
        var order = new List<string>();
        bus.SubscribeAfter<DamageEvent>(_ => order.Add("after"));

        bus.Raise(new DamageEvent { Amount = 10 }, _ => order.Add("base"));

        Assert.Equal(new[] { "base", "after" }, order);
    }

    [Fact]
    public void ThrowingHandler_DoesNotCrash()
    {
        var bus = new GameEventBus();
        bus.SubscribeAfter<DamageEvent>(_ => throw new InvalidOperationException());
        bus.SubscribeAfter<DamageEvent>(_ => Assert.True(true));

        var result = bus.Raise(new DamageEvent { Amount = 1 }, _ => { });

        Assert.True(result);
    }
}

public class ContentRegistryTests
{
    private sealed class Hero
    {
        public string Id { get; set; } = string.Empty;
        public int Health { get; set; }
    }

    [Fact]
    public void Register_ThenGet_ReturnsSameInstance()
    {
        var registry = new ContentRegistry();
        var hero = new Hero { Id = "hero1", Health = 100 };

        Assert.True(registry.Register("hero1", hero).IsSuccess);

        var result = registry.Get<Hero>("hero1");
        Assert.True(result.IsSuccess);
        Assert.Same(hero, result.Value);
    }

    [Fact]
    public void Register_ByExplicitId_IsIndependentFromObjectId()
    {
        var registry = new ContentRegistry();
        var hero = new Hero { Id = "internal", Health = 50 };

        Assert.True(registry.Register("external", hero).IsSuccess);
        Assert.True(registry.Get<Hero>("external").IsSuccess);
        Assert.True(registry.Get<Hero>("internal").IsFailure);
    }

    [Fact]
    public void Get_MissingId_ReturnsFailure()
    {
        var registry = new ContentRegistry();
        Assert.True(registry.Get<Hero>("nope").IsFailure);
    }

    [Fact]
    public void Get_WrongType_ReturnsFailure()
    {
        var registry = new ContentRegistry();
        registry.Register("x", new Hero { Id = "x" });

        Assert.True(registry.Get<GameLog>("x").IsFailure);
    }
}
