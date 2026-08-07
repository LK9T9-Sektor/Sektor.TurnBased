namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Результат изменения стата: неизменяемый снимок изменения.
/// Содержит только данные (id, дельту, новое значение) — без ссылок на актора,
/// чтобы безопасно жить в шине событий и очереди визуализации.
/// </summary>
public sealed record StatChange(string StatId, int Delta, int NewValue);
