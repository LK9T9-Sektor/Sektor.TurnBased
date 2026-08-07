namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Внешний вид юнита для отображения: ключ иконки и цвет в формате "#RRGGBB".
/// Строковые данные — иконки резолвит хост (WPF/Unity), цвет парсит конвертер.
/// Используются переопределения по TemplateId либо фолбэк по команде.
/// </summary>
public sealed record UnitAppearance(string IconKey, string ColorHex);

/// <summary>
/// Реестр внешнего вида юнитов: сопоставляет TemplateId юнита иконке и цвету.
/// Каталог демо-контента (ростер из Blades); неизвестный шаблон — по команде.
/// </summary>
public static class UnitAppearances
{
    private static readonly IReadOnlyDictionary<string, UnitAppearance> ByTemplate =
        new Dictionary<string, UnitAppearance>
        {
            ["hero_warrior"] = new("IconShield", "#4682B4"),
            ["hero_rogue"] = new("IconDagger", "#DAA520"),
            ["hero_archer"] = new("IconArcher", "#B8860B"),
            ["hero_priestess"] = new("IconPriestess", "#808080"),
            ["skeleton"] = new("IconSkeleton", "#800000"),
            ["zombie"] = new("IconZombie", "#8B0000"),
            ["skeleton_archer"] = new("IconSkeleton", "#8FBC8F"),
            ["skeleton_mage"] = new("IconSkeletonMage", "#808000"),
        };

    private static readonly IReadOnlyDictionary<string, UnitAppearance> ByTeam =
        new Dictionary<string, UnitAppearance>
        {
            ["player"] = new("IconShield", "#4682B4"),
            ["enemy"] = new("IconSkeleton", "#800000"),
        };

    /// <summary>Внешний вид юнита по шаблону; фолбэк — по команде/по умолчанию.</summary>
    public static UnitAppearance ForTemplate(string templateId, string teamId)
    {
        if (ByTemplate.TryGetValue(templateId, out var appearance))
            return appearance;
        if (ByTeam.TryGetValue(teamId, out var fallback))
            return fallback;
        return new UnitAppearance("IconShield", "#808080");
    }
}
