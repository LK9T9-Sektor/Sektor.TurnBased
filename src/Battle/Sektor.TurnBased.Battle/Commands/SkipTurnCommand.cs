using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Commands;

/// <summary>
/// Команда «пропустить ход» от игрока: текущий актор ходит без действия.
/// Позволяет завершить ход героя, когда действие не нужно или невыгодно.
/// </summary>
public sealed record SkipTurnCommand(string ActorRuntimeId) : IGameCommand;
