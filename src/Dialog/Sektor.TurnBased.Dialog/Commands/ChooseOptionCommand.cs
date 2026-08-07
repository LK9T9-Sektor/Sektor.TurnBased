using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Dialog.Commands;

/// <summary>
/// Команда «выбрать вариант ответа» от игрока. NodeId — узел, где выбран вариант
/// (должен совпадать с текущим), ChoiceId — идентификатор варианта внутри узла.
/// </summary>
public sealed record ChooseOptionCommand(string NodeId, string ChoiceId) : IGameCommand;
