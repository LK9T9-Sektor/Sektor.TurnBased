namespace Sektor.TurnBased.Core.Abstractions;

/// <summary>
/// Маркер-контракт команды от игрока/UI/сети.
/// Пустой намеренно: каждая игра определяет собственные команды и их данные.
/// Идентификация отправителя выносится в обёртку на уровне сессии.
/// </summary>
public interface IGameCommand
{
}
