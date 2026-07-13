namespace Sektor.TurnBased.GameCore.Actions;

/// <summary>Плоское представление RuntimeInstance для безопасной сериализации.</summary>
public class ActionState
{
    public string RuntimeId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public int CurrentCooldown { get; set; }
}
