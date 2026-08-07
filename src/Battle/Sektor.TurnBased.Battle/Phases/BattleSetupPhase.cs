using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Phases;

/// <summary>
/// Фаза настройки боя: создаёт акторов из шаблонов и добавляет в состояние.
/// Порядок ходов не вычисляется здесь — его пересчитывает RoundStart.
/// </summary>
public sealed class BattleSetupPhase : IGamePhase
{
    private readonly BattleState _state;
    private readonly BattleContent _content;

    public string Id => BattlePhaseIds.Setup;

    public BattleSetupPhase(BattleState state, BattleContent content)
    {
        _state = state;
        _content = content;
    }

    public Result<PhaseTransition> Execute(GameContext context)
    {
        foreach (var template in _content.Templates)
        {
            var resources = new ResourceContainer(_state.Definitions);
            foreach (var pair in template.BaseStats)
            {
                var result = resources.SetInitial(pair.Key, pair.Value);
                if (result.IsFailure)
                    return Result<PhaseTransition>.Failure(result.Error!);
            }

            var actor = new BattleActor(
                _state.NewActorId(template.Id),
                template.TeamId,
                template.Id,
                template.ControlledBy,
                resources);

            _state.AddActor(actor);
            context.Visuals.Enqueue(new VisualEvent
            {
                EventType = "Spawn",
                SourceRuntimeId = actor.RuntimeId,
                TargetRuntimeId = actor.RuntimeId,
                Payload = template.TeamId,
            });
            context.Log.Append($"{actor.RuntimeId} ({template.Id}) joined team {template.TeamId}");
        }

        return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.RoundStart));
    }
}
