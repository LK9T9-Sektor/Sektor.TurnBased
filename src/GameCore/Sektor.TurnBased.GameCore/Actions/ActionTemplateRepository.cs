using Sektor.TurnBased.GameCore.Extensions;

namespace Sektor.TurnBased.GameCore.Actions;

/// <summary>
/// Хранилище шаблонов действий. Repository Pattern.
/// Принимает готовые объекты от слоя GameLogic. Не занимается IO или парсингом.
/// </summary>
public sealed class ActionTemplateRepository
{
    private readonly Dictionary<string, BaseActionTemplate> _templates = new();

    public Result<T> Register<T>(T template) where T : BaseActionTemplate
    {
        if (string.IsNullOrWhiteSpace(template.Id))
            return Result<T>.Failure("Template ID cannot be empty.");
        _templates[template.Id] = template;
        return Result<T>.Success(template);
    }

    public void Register(IEnumerable<BaseActionTemplate> templates)
    {
        foreach (BaseActionTemplate t in templates)
            if (!string.IsNullOrWhiteSpace(t.Id)) _templates[t.Id] = t;
    }

    public Result<T> Get<T>(string id) where T : BaseActionTemplate
    {
        if (!_templates.TryGetValue(id, out BaseActionTemplate? t))
            return Result<T>.Failure($"Action '{id}' not found.");
        return t is T typed ? Result<T>.Success(typed) : Result<T>.Failure($"Type mismatch for '{id}'.");
    }

    public bool TryGet<T>(string id, out T? template) where T : BaseActionTemplate
    {
        if (_templates.TryGetValue(id, out BaseActionTemplate? raw) && raw is T typed)
        {
            template = typed;
            return true;
        }
        template = default;
        return false;
    }

    public void Clear() => _templates.Clear();
    public int Count => _templates.Count;
}