namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Определение статуса: какие статы модифицирует, тик-эффект (выполняется в начале раунда)
/// и блокирует ли ход. Данные, не поведение.
/// </summary>
public sealed record StatusDefinition(
    string Id,
    IReadOnlyDictionary<string, int> StatModifiers,
    int Duration,
    string? TickEffectId = null,
    bool BlocksTurn = false);
