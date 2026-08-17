using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Phases;

/// <summary>
/// Фаза настройки боя: создаёт акторов по ростеру спавнов и добавляет в состояние.
/// Ростер определяет состав боя (слоты игроков + AI); по умолчанию — все шаблоны.
/// Порядок ходов не вычисляется здесь — его пересчитывает RoundStart.
/// </summary>
public sealed class BattleSetupPhase : IGamePhase
{
    private readonly BattleState _state;
    private readonly BattleContent _content;
    private readonly IReadOnlyList<BattleSpawn> _spawns;

    public string Id => BattlePhaseIds.Setup;

    public BattleSetupPhase(BattleState state, BattleContent content, IReadOnlyList<BattleSpawn> spawns)
    {
        _state = state;
        _content = content;
        _spawns = spawns;
    }

    public Result<PhaseTransition> Execute(GameContext context)
    {
        foreach (var spawn in _spawns)
        {
            var template = _content.Templates.FirstOrDefault(t => t.Id == spawn.TemplateId);
            if (template is null)
                return Result<PhaseTransition>.Failure($"Spawn references unknown template '{spawn.TemplateId}'.");

            var resources = new ResourceContainer(_state.Definitions);
            foreach (var pair in template.BaseStats)
            {
                var result = resources.SetInitial(pair.Key, pair.Value);
                if (result.IsFailure)
                    return Result<PhaseTransition>.Failure(result.Error!);
            }

            var actor = new BattleActor(
                _state.NewActorId(spawn.TemplateId),
                spawn.TeamId,
                spawn.TemplateId,
                spawn.ControlledBy,
                resources);

            _state.AddActor(actor);
            context.Visuals.Enqueue(new VisualEvent
            {
                EventType = "Spawn",
                SourceRuntimeId = actor.RuntimeId,
                TargetRuntimeId = actor.RuntimeId,
                Payload = spawn.TeamId,
            });
            context.Log.Append($"{actor.RuntimeId} ({spawn.TemplateId}) joined team {spawn.TeamId}");
        }

        return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.RoundStart));
    }
}
