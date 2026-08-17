namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Отображение игрока в мультиплеерном бою: имя и цвет для карточек юнитов его
/// слота. Индексируется по слоту (ControlledBy "player_N").
/// </summary>
public sealed record PlayerPresentation(string Name, string ColorHex);