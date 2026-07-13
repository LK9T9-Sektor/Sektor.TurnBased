using Sektor.TurnBased.GameCore.Actions;

namespace Sektor.TurnBased.GameCore.Runtime;

/// <summary>
/// Экземпляр действия в бою.
/// </summary>
public class RuntimeInstance<TTemplate>(TTemplate template) where TTemplate : BaseActionTemplate
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public TTemplate Template { get; } = template;
    public int Cooldown { get; set; }

    public bool IsReady => Cooldown <= 0 && Template.IsEnabled;

    public void Tick() { if (Cooldown > 0) Cooldown--; }
    public void SetCooldown(int turns) => Cooldown = turns;
}