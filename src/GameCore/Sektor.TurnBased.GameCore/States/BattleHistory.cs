using Sektor.TurnBased.GameCore.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sektor.TurnBased.GameCore.States;

/// <summary>
/// Хранит снимки состояния боя для отката ходов. Паттерн Memento.
/// Работает исключительно с BattleState через JSON-сериализацию.
/// </summary>
public sealed class BattleHistory
{
    private readonly Stack<string> _snapshots = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Result<bool> SaveSnapshot(BattleState state)
    {
        _snapshots.Push(JsonSerializer.Serialize(state, _jsonOptions));
        return Result<bool>.Success(true);
    }

    public Result<bool> RestoreLast(BattleState state)
    {
        if (!_snapshots.TryPop(out string? json))
            return Result<bool>.Failure("No snapshots in history.");

        BattleState? restored = JsonSerializer.Deserialize<BattleState>(json, _jsonOptions);
        if (restored is null)
            return Result<bool>.Failure("Deserialization failed.");

        CopyState(restored, state);
        return Result<bool>.Success(true);
    }

    private static void CopyState(BattleState source, BattleState target)
    {
        target.TurnNumber = source.TurnNumber;
        target.Seed = source.Seed;
        target.ActiveActorId = source.ActiveActorId;
        target.ActorIds.Clear(); target.ActorIds.AddRange(source.ActorIds);
        target.TurnOrder.Clear(); target.TurnOrder.AddRange(source.TurnOrder);
        target.Zones.Clear();
        foreach (KeyValuePair<string, List<string>> entry in source.Zones)
            target.Zones[entry.Key] = new(entry.Value);
        target.CombatLog.Clear(); target.CombatLog.AddRange(source.CombatLog);
    }

    public void Clear() => _snapshots.Clear();
    public int Depth => _snapshots.Count;
}