namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Снимок состояния боя для отображения: раунд, текущий актор, все акторы и
/// доступные действия текущего игрока. Не содержит ссылок на движок.
/// </summary>
public sealed record BattleSnapshot(
    string PhaseId,
    int RoundNumber,
    int TurnIndex,
    string? CurrentActorId,
    string? WinnerTeamId,
    IReadOnlyList<UnitSnapshot> Actors,
    IReadOnlyList<ActionOption> AvailableActions);
