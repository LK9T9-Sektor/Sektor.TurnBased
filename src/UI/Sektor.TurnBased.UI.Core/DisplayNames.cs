using System.Text;

namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Отображаемые имена для идентификаторов контента: используются переопределения
/// (передаются в сессию) либо читаемая форма Id (Humanize).
/// </summary>
public static class DisplayNames
{
    /// <summary>
    /// Читаемая форма идентификатора: "hero_warrior" -> "Hero Warrior".
    /// Пустой/пробельный Id даёт пустую строку.
    /// </summary>
    public static string Humanize(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return string.Empty;

        var words = id.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();
        foreach (var word in words)
        {
            if (builder.Length > 0)
                builder.Append(' ');
            builder.Append(char.ToUpperInvariant(word[0]));
            builder.Append(word, 1, word.Length - 1);
        }

        return builder.ToString();
    }
}
