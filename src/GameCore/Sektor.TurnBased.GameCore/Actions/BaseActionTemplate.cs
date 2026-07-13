namespace Sektor.TurnBased.GameCore.Actions;

/// <summary>
/// Базовый полиморфный шаблон действия/карты/навыка. Сериализуется из JSON.
/// </summary>
public abstract class BaseActionTemplate
{
    /// <summary>Уникальный идентификатор шаблона (ключ в JSON).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Читаемое название для UI и логов.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание эффекта или механики.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Список ID дополнительных эффектов (триггеры, баффы, условия).</summary>
    public List<string> EffectIds { get; set; } = new();

    /// <summary>Флаг включения/выключения модуля или конкретного действия.</summary>
    public bool IsEnabled { get; set; } = true;
}