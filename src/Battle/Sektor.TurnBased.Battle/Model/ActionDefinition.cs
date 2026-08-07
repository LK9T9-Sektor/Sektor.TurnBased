namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Определение действия: режим цели, упорядоченный список прекондиций и эффектов.
/// Порядок прекондиций и эффектов — контракт и часть конфигурации (все прекондиции
/// должны пройти до применения, эффекты применяются по порядку).
/// </summary>
public sealed record ActionDefinition(
    string Id,
    string Name,
    string TargetMode,
    IReadOnlyList<string> Preconditions,
    IReadOnlyList<string> Effects);
