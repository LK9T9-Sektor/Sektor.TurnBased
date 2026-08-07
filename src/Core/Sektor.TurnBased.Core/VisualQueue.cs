namespace Sektor.TurnBased.Core;

/// <summary>
/// FIFO-очередь визуальных событий. Ядро добавляет, UI последовательно считывает и анимирует.
/// </summary>
public sealed class VisualQueue
{
    private readonly Queue<VisualEvent> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(VisualEvent evt)
    {
        if (evt is not null)
            _queue.Enqueue(evt);
    }

    public bool TryDequeue(out VisualEvent? evt) => _queue.TryDequeue(out evt);

    public void Clear() => _queue.Clear();
}
