namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Спавн актора при настройке боя: шаблон, команда и способ управления.
/// Ростер позволяет собрать бой из назначений игроков (слоты player_N), а не
/// из всех шаблонов каталога.
/// </summary>
public sealed record BattleSpawn(
    string TemplateId,
    string TeamId,
    string ControlledBy);