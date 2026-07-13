namespace Sektor.TurnBased.GameCore.Actors;

/// <summary>
/// Базовый полиморфный шаблон юнита/персонажа. Содержит статы и параметры.
/// </summary>
public abstract class BaseActorTemplate
{
    /// <summary>Уникальный идентификатор шаблона.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Название юнита.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Базовая скорость для расчёта очереди ходов.</summary>
    public int BaseSpeed { get; set; }

    /// <summary>Базовое максимальное количество здоровья.</summary>
    public int BaseHP { get; set; }

    /// <summary>Флаг активности шаблона (для отключения модами/балансом).</summary>
    public bool IsEnabled { get; set; } = true;
}