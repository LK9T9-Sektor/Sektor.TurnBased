using System.Collections.Concurrent;

namespace Sektor.TurnBased.GameCore.Visuals;

/// <summary>
/// Потокобезопасная очередь визуальных событий.
/// Ядро добавляет события, UI последовательно считывает и анимирует их.
/// </summary>
public sealed class VisualQueue
{
    private readonly ConcurrentQueue<VisualEvent> _queue = new();

    /// <summary>Добавляет визуальное событие в конец очереди.</summary>
    public void Enqueue(VisualEvent evt) => _queue.Enqueue(evt);

    /// <summary>Пытается извлечь следующее событие для отрисовки.</summary>
    public bool TryDequeue(out VisualEvent evt) => _queue.TryDequeue(out evt);

    /// <summary>Количество ожидающих анимаций.</summary>
    public int Count => _queue.Count;

    /// <summary>Очищает очередь (например, при рестарте матча).</summary>
    public void Clear() { while (_queue.TryDequeue(out _)) ; }
}