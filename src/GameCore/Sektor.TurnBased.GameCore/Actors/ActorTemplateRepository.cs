using Sektor.TurnBased.GameCore.Extensions;

namespace Sektor.TurnBased.GameCore.Actors;

/// <summary>
/// Чистое in-memory хранилище. Не знает о JSON, файлах или путях.
/// </summary>
public sealed class ActorTemplateRepository
{
    private readonly Dictionary<string, BaseActorTemplate> _actors = new();

    public Result<T> Register<T>(T template) where T : BaseActorTemplate
    {
        if (string.IsNullOrWhiteSpace(template.Id))
            return Result<T>.Failure("Template ID cannot be empty.");

        _actors[template.Id] = template;
        return Result<T>.Success(template);
    }

    public void Register(IEnumerable<BaseActorTemplate> templates)
    {
        foreach (var template in templates)
        {
            if (!string.IsNullOrWhiteSpace(template.Id))
                _actors[template.Id] = template;
        }
    }

    public Result<T> Get<T>(string id) where T : BaseActorTemplate
    {
        if (!_actors.TryGetValue(id, out var template))
            return Result<T>.Failure($"Actor template '{id}' not found.");

        return template is T typed
            ? Result<T>.Success(typed)
            : Result<T>.Failure($"Type mismatch for '{id}'.");
    }

    public bool TryGet<T>(string id, out T? template) where T : BaseActorTemplate
    {
        if (_actors.TryGetValue(id, out var raw) && raw is T typed)
        {
            template = typed;
            return true;
        }

        template = default;
        return false;
    }

    public void Clear() => _actors.Clear();
    public int Count => _actors.Count;
}