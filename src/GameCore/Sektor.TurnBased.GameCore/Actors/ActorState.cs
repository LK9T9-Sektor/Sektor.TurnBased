namespace Sektor.TurnBased.GameCore.Actors;

/// <summary>Плоское представление RuntimeActor для безопасной сериализации.</summary>
public class ActorState
{
    public string RuntimeId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty; // Полное имя типа шаблона
    public int CurrentHP { get; set; }
    public string? ZoneId { get; set; }
    /// <summary>Сериализация статусов: "StunStatus" -> 2.</summary>
    public Dictionary<string, int> Statuses { get; set; } = new();
}
