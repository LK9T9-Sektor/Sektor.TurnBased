using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Драйвер сессии: продвигает игру до ожидания ввода или завершения. Полезен для
/// сценариев «шагнули и остановились» и для тестов.
/// </summary>
public static class SessionDriver
{
    /// <summary>Продвигает сессию до ожидания ввода, завершения или ошибки.</summary>
    public static Result Step(GameSession session)
    {
        while (!session.IsFinished && !session.NeedsInput && !session.IsFailed)
        {
            var advanced = session.Advance();
            if (advanced.IsFailure)
                return advanced;
        }

        return Result.Success();
    }
}
