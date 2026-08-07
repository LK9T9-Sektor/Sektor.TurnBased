namespace Sektor.TurnBased.Core.Abstractions;

/// <summary>
/// Маркер-контракт состояния игры.
/// Пустой намеренно: ядро не навязывает игровых данных.
/// Каждая игра определяет собственное конкретное состояние и несёт только свои поля.
/// </summary>
public interface IGameState
{
}
