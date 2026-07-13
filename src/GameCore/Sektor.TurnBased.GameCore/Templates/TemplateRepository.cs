using Sektor.TurnBased.GameCore.Actions;
using Sektor.TurnBased.GameCore.Actors;
using Sektor.TurnBased.GameCore.Extensions;

namespace Sektor.TurnBased.GameCore.Templates;

/// <summary>
/// Чистое рантайм-хранилище шаблонов. Не знает о файлах, JSON или структуре папок.
/// Принимает только готовые объекты от слоя конкретной игры.
/// </summary>
public sealed class TemplateRepository
{
    private readonly Dictionary<string, BaseActorTemplate> _actors = new();
    private readonly Dictionary<string, BaseActionTemplate> _actions = new();

    public void RegisterActor<T>(T template) where T : BaseActorTemplate
    {
        if (string.IsNullOrWhiteSpace(template.Id)) return;
        _actors[template.Id] = template;
    }

    public void RegisterAction<T>(T template) where T : BaseActionTemplate
    {
        if (string.IsNullOrWhiteSpace(template.Id)) return;
        _actions[template.Id] = template;
    }

    public void RegisterActors(IEnumerable<BaseActorTemplate> templates)
    {
        foreach (var t in templates) RegisterActor(t);
    }

    public void RegisterActions(IEnumerable<BaseActionTemplate> templates)
    {
        foreach (var t in templates) RegisterAction(t);
    }

    public Result<T> GetActor<T>(string id) where T : BaseActorTemplate
    {
        if (!_actors.TryGetValue(id, out var t)) return Result<T>.Failure($"Actor '{id}' not found.");
        if (t is not T typed) return Result<T>.Failure($"Type mismatch for '{id}'.");
        return Result<T>.Success(typed);
    }

    public Result<T> GetAction<T>(string id) where T : BaseActionTemplate
    {
        if (!_actions.TryGetValue(id, out var t)) return Result<T>.Failure($"Action '{id}' not found.");
        if (t is not T typed) return Result<T>.Failure($"Type mismatch for '{id}'.");
        return Result<T>.Success(typed);
    }

    public void Clear() { _actors.Clear(); _actions.Clear(); }
}