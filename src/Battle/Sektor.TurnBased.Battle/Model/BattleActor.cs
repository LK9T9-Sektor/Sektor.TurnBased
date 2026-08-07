using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Статус на акторе: состояние через ResourceContainer и список статусов.
/// Чистая сущность: поведение — через эффекты и фазы. Внешний мир читает свойства.
/// </summary>
public sealed class BattleActor
{
    private readonly List<BattleStatus> _statuses = new();

    public string RuntimeId { get; }
    public string TeamId { get; }
    public string TemplateId { get; }
    public string ControlledBy { get; }
    public ResourceContainer Resources { get; }
    public IReadOnlyList<BattleStatus> Statuses => _statuses;

    public BattleActor(
        string runtimeId,
        string teamId,
        string templateId,
        string controlledBy,
        ResourceContainer resources)
    {
        RuntimeId = runtimeId;
        TeamId = teamId;
        TemplateId = templateId;
        ControlledBy = controlledBy;
        Resources = resources;
    }

    /// <summary>Добавляет статус на актора.</summary>
    public Result AddStatus(BattleStatus status)
    {
        if (status is null)
            return Result.Failure("Status cannot be null.");
        _statuses.Add(status);
        return Result.Success();
    }

    /// <summary>Удаляет истёкшие статусы.</summary>
    public void RemoveExpiredStatuses() => _statuses.RemoveAll(s => s.IsExpired);
}
