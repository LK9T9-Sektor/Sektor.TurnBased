namespace Sektor.TurnBased.Core;

/// <summary>
/// Описание визуального события для очереди анимаций UI.
/// Позволяет отделить мгновенную логику ядра от плавной отрисовки.
/// EventType — строка ("Damage", "Heal", "StatusApply", "Shake", ...), а не enum.
/// </summary>
public sealed class VisualEvent
{
    public string EventType { get; set; } = string.Empty;

    /// <summary>ID инициатора (SourceActorId или ActionId).</summary>
    public string SourceRuntimeId { get; set; } = string.Empty;

    /// <summary>ID цели (может быть null для глобальных эффектов).</summary>
    public string? TargetRuntimeId { get; set; }

    public int Value { get; set; }

    /// <summary>Дополнительные данные для рендерера (координаты, цвета, тексты).</summary>
    public object? Payload { get; set; }
}
