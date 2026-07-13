using Sektor.TurnBased.GameCore.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sektor.TurnBased.GameCore.Battles;

/// <summary>
/// Хранит историю снимков состояния боя для отката ходов.
/// Реализует паттерн Memento.
/// </summary>
public sealed class BattleHistory
{
    private readonly Stack<string> _snapshots = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Сохраняет текущий снимок состояния в историю.
    /// </summary>
    public Result<bool> SaveSnapshot(BattleState state)
    {
        _snapshots.Push(JsonSerializer.Serialize(state, _jsonOptions));
        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Восстанавливает последнее сохранённое состояние.
    /// </summary>
    public Result<bool> RestoreLast(BattleState state)
    {
        if (!_snapshots.TryPop(out var json))
            return Result<bool>.Failure("No snapshots in history.");

        var restored = JsonSerializer.Deserialize<BattleState>(json, _jsonOptions);
        if (restored is null)
            return Result<bool>.Failure("Failed to deserialize snapshot.");

        // Копируем данные в существующий объект (чтобы не ломать ссылки)
        CopyState(restored, state);
        return Result<bool>.Success(true);
    }

    private static void CopyState(BattleState source, BattleState target)
    {
        target.TurnNumber = source.TurnNumber;
        target.Seed = source.Seed;
        target.CurrentStepId = source.CurrentStepId;
        target.ActiveActorId = source.ActiveActorId;
        target.ActorIds.Clear(); target.ActorIds.AddRange(source.ActorIds);
        target.TurnOrder.Clear(); target.TurnOrder.AddRange(source.TurnOrder);
        target.Zones.Clear();
        foreach (var (k, v) in source.Zones) target.Zones[k] = new(v);
        target.CombatLog.Clear(); target.CombatLog.AddRange(source.CombatLog);
    }

    public void Clear() => _snapshots.Clear();
    public int Depth => _snapshots.Count;
}