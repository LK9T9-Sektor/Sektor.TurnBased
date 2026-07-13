namespace Sektor.TurnBased.GameCore.Actions;

/// <summary>
/// Базовый контракт для шаблонов действий/карт/навыков.
/// Конкретные игры наследуются от этого класса и добавляют свои поля (Cost, Range, Cooldown).
/// </summary>
public abstract class BaseActionTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}