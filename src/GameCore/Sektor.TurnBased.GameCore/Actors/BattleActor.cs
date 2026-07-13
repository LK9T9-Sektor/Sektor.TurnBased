namespace Sektor.TurnBased.GameCore.Actors;

/// <summary>
/// Живой участник боя. Хранит изменяемое состояние.
/// </summary>
public class BattleActor<TTemplate>(TTemplate template) where TTemplate : BaseActorTemplate
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public TTemplate Template { get; } = template;
    public int CurrentHp { get; set; } = template.BaseHP;
    public string? ZoneId { get; set; }
    public Dictionary<Type, int> Statuses { get; } = new();

    public bool IsDead => CurrentHp <= 0;

    public void ModifyHp(int amount) => CurrentHp = Math.Max(0, CurrentHp + amount);

    public void AddStatus<TStatus>(int stacks = 1) where TStatus : class
    {
        var type = typeof(TStatus);
        Statuses.TryGetValue(type, out var current);
        Statuses[type] = current + stacks;
    }

    public int GetStatusStacks<TStatus>() where TStatus : class =>
        Statuses.TryGetValue(typeof(TStatus), out var stacks) ? stacks : 0;

    public void RemoveStatus<TStatus>() where TStatus : class =>
        Statuses.Remove(typeof(TStatus));
}