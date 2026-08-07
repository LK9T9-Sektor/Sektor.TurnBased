namespace Sektor.TurnBased.Core;

/// <summary>
/// Текстовый журнал событий игры (для лога, отладки и UI).
/// Это рантайм-сервис, а не игровые данные: не сериализуется в состояние.
/// </summary>
public sealed class GameLog
{
    private readonly List<string> _entries = new();

    public IReadOnlyList<string> Entries => _entries;

    public void Append(string entry)
    {
        if (!string.IsNullOrWhiteSpace(entry))
            _entries.Add(entry);
    }

    public void Clear() => _entries.Clear();
}
