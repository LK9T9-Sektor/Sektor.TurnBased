namespace Sektor.TurnBased.Battle.Events;

/// <summary>
/// Доменное событие: изменение стата актора. Поднимается через GameEventBus ядра
/// с базовой логикой «визуализация + лог». Правило: события поднимаются только из
/// фаз и исполнителя, не из обработчиков шины (защита от циклов).
/// </summary>
public sealed record ActorStatChanged(string ActorRuntimeId, string StatId, int Delta, int NewValue);
