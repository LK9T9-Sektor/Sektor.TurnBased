namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Определение стата (ресурса) актора в бою.
/// Данные, а не поведение: поведение задают эффекты.
/// Стат идентифицируется строковым Id, поэтому не является enum.
/// </summary>
public sealed record StatDefinition(
    string Id,
    string Name,
    int? Min = null,
    int? Max = null,
    bool ClampMin = false,
    bool IsDeathStat = false,
    int? TurnRegen = null);
