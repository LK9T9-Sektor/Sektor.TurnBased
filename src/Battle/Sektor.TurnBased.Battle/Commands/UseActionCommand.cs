using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Commands;

/// <summary>
/// Команда «применить действие» от игрока или AI. Record-DTO: источник, действие, цели.
/// Один и тот же тип возвращают игрок (через UI) и AI — ядро их не различает.
/// </summary>
public sealed record UseActionCommand(
    string ActorRuntimeId,
    string ActionId,
    IReadOnlyList<string> TargetActorIds) : IGameCommand;
