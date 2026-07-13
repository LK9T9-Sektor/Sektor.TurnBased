using Sektor.TurnBased.GameCore.Extensions;

namespace Sektor.TurnBased.GameCore.Actions;

/// <summary>
/// Хранилище шаблонов действий. Отвечает только за регистрацию и поиск.
/// Полностью изолирован от акторов и файловой системы.
/// </summary>
public sealed class ActionTemplateRepository
{
    private readonly Dictionary<string, BaseActionTemplate> _actions = new();

    public Result<T> Register<T>(T template) where T : BaseActionTemplate
    {
        if (string.IsNullOrWhiteSpace(template.Id))
            return Result<T>.Failure("Template ID cannot be empty.");

        _actions[template.Id] = template;
        return Result<T>.Success(template);
    }

    public void Register(IEnumerable<BaseActionTemplate> templates)
    {
        foreach (var template in templates)
        {
            if (!string.IsNullOrWhiteSpace(template.Id))
                _actions[template.Id] = template;
        }
    }

    public Result<T> Get<T>(string id) where T : BaseActionTemplate
    {
        if (!_actions.TryGetValue(id, out var template))
            return Result<T>.Failure($"Action template '{id}' not found.");

        return template is T typed
            ? Result<T>.Success(typed)
            : Result<T>.Failure($"Template '{id}' is not of type {typeof(T).Name}.");
    }

    public bool TryGet<T>(string id, out T? template) where T : BaseActionTemplate
    {
        if (_actions.TryGetValue(id, out var raw) && raw is T typed)
        {
            template = typed;
            return true;
        }

        template = default;
        return false;
    }

    public void Clear() => _actions.Clear();
    public int Count => _actions.Count;
}