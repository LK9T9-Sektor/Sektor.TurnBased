namespace Sektor.TurnBased.GameCore.Entities;

/// <summary>
/// Живой участник боя. Хранит только изменяемые данные.
/// Специфичные статы (Скорость, ОД, CR) вынесены в Attributes для избежания мёртвых полей.
/// </summary>
public class BattleActor
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Ссылка на статический JSON-шаблон.</summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>Текущее здоровье. Универсально для большинства игр.</summary>
    public int CurrentHp { get; set; }

    /// <summary>Текущая зона актёра. Дублирует данные из BattleState.Zones для быстрого доступа.</summary>
    public string? ZoneId { get; set; }

    /// <summary>Статусы/баффы/дебаффы. Ключ: Type, Значение: стаки.</summary>
    public Dictionary<Type, int> Statuses { get; set; } = new();

    /// <summary>
    /// Гибкое хранилище игроспецифичных данных.
    /// Примеры: "Speed" -> 10, "ActionPoints" -> 5, "CombatReadiness" -> 80.
    /// Отсутствие ключа означает, что игра не использует данную механику.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();

    public bool IsDead => CurrentHp <= 0;

    public void ModifyHp(int amount) => CurrentHp = Math.Max(0, CurrentHp + amount);

    public T? GetAttribute<T>(string key) =>
        Attributes.TryGetValue(key, out object? value) && value is T result ? result : default;

    public void SetAttribute<T>(string key, T value) => Attributes[key] = value!;
}