namespace Sektor.TurnBased.GameCore.Visuals;

/// <summary>
/// Описание визуального события для очереди анимаций UI.
/// Позволяет отделить мгновенную логику ядра от плавной отрисовки.
/// </summary>
public class VisualEvent
{
    /// <summary>Тип события: "Damage", "Heal", "StatusApply", "Shake", "Flash" и т.д.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>ID инициатора (SourceActorId или ActionId).</summary>
    public string SourceRuntimeId { get; set; } = string.Empty;

    /// <summary>ID цели (может быть null для глобальных эффектов).</summary>
    public string? TargetRuntimeId { get; set; }

    /// <summary>Числовое значение (урон, лечение, стаки).</summary>
    public int Value { get; set; }

    /// <summary>Дополнительные данные для рендерера (координаты, цвета, тексты).</summary>
    public object? Payload { get; set; }
}