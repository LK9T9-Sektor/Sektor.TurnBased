using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>
/// Прекондиция действия: проверяет, может ли действие быть применено.
/// Упорядоченный список прекондиций — цепочка проверок (CoR): все должны пройти.
/// </summary>
public interface ICombatPrecondition
{
    string Id { get; }

    Result<bool> CanApply(ActionContext context);
}
