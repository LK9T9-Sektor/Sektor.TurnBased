namespace Sektor.TurnBased.UI.Core.Multiplayer;

/// <summary>Типы игровых сообщений боя поверх лобби-протокола.</summary>
public static class BattleMessageTypes
{
    public const string PlayerSelectClass = "player_select_class";
    public const string StartGame = "start_game";
    public const string BattleCommand = "battle_command";
    public const string BattleState = "battle_state";
}

/// <summary>Клиент выбирает класс героя.</summary>
public sealed record SelectClassMessage(string ClassId);

/// <summary>Хост запускает игру: seed для детерминированного боя и назначения героев.</summary>
public sealed record StartGameMessage(int Seed, IReadOnlyList<PlayerAssignment> Assignments);

/// <summary>Назначение героя игроку.</summary>
public sealed record PlayerAssignment(string PlayerId, string ClassId);

/// <summary>Команда боя по сети (lockstep): все клиенты применяют её локально.</summary>
public sealed record BattleCommandMessage(
    string ActorRuntimeId,
    string? ActionId,
    IReadOnlyList<string> TargetActorIds);