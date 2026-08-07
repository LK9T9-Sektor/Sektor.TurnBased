namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Снимок значения стата юнита для отображения: идентификатор, имя, текущее и
/// максимальное значение (максимум может отсутствовать — неограниченный ресурс).
/// </summary>
public sealed record StatValueSnapshot(
    string StatId,
    string Name,
    int Current,
    int? Max);
