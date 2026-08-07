using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>
/// Эффект боевого действия: применяет изменение состояния (только процессинг).
/// Оценка урона (EstimateDamage) — чистая функция для AI, без побочных эффектов.
/// </summary>
public interface ICombatEffect
{
    string Id { get; }

    Result Apply(ActionContext context);

    /// <summary>Ожидаемый прямой урон по цели (для AI). По умолчанию 0.</summary>
    int EstimateDamage(ActionContext context, string targetActorId) => 0;
}
