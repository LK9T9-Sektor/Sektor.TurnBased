namespace Sektor.TurnBased.Core.Abstractions;

/// <summary>
/// Результат выполнения фазы: перейти к другой фазе, приостановиться (ожидание ввода)
/// или завершить пайплайн. Без исключений.
/// </summary>
public sealed class PhaseTransition
{
    /// <summary>ID следующей фазы (null, если это не переход).</summary>
    public string? NextPhaseId { get; }

    /// <summary>Причина приостановки (null, если фаза не приостановлена).</summary>
    public string? SuspendReason { get; }

    /// <summary>true — фаза продолжает выполнение после команды (Resume).</summary>
    public bool IsResume { get; }

    /// <summary>true — пайплайн завершён.</summary>
    public bool IsFinished { get; }

    public bool IsSuspended => SuspendReason is not null;

    private PhaseTransition(string? nextPhaseId, string? suspendReason, bool isFinished, bool isResume)
    {
        NextPhaseId = nextPhaseId;
        SuspendReason = suspendReason;
        IsFinished = isFinished;
        IsResume = isResume;
    }

    /// <summary>Перейти к следующей фазе по её ID.</summary>
    public static PhaseTransition Next(string phaseId) => new(phaseId, null, false, false);

    /// <summary>Приостановиться до команды/события. Причина по умолчанию — ожидание ввода.</summary>
    public static PhaseTransition Suspend(string? reason = null) => new(null, reason ?? "awaiting_input", false, false);

    /// <summary>Продолжить выполнение текущей фазы (после обработки команды).</summary>
    public static PhaseTransition Resume() => new(null, null, false, true);

    /// <summary>Завершить пайплайн.</summary>
    public static PhaseTransition Finish() => new(null, null, true, false);
}
