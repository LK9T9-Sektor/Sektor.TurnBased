namespace Sektor.TurnBased.UI.Core.Multiplayer;

/// <summary>
/// Сетевая боевая сессия: применяет входящие команды, сигнализирует об изменении
/// состояния (для refresh VM) и прокачивается через Update (таймер/кадр).
/// </summary>
public interface INetworkedBattleSession
{
    /// <summary>Состояние изменилось после применения входящей команды.</summary>
    event Action? StateChanged;

    /// <summary>Прокачивает транспорт и применяет буферизованные входящие команды.</summary>
    void Update();
}