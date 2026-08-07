using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Core;

/// <summary>
/// Реестр контента: ID → один или несколько объектов.
/// Никогда не бросает исключений: ошибки возвращаются через Result.
/// </summary>
public sealed class ContentRegistry
{
    private readonly Dictionary<string, List<object>> _items = new();

    public int Count => _items.Count;

    /// <summary>Регистрирует контент по явному ID.</summary>
    public Result Register(string id, object content)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Result.Failure("Content id cannot be empty.");
        if (content is null)
            return Result.Failure("Content cannot be null.");

        if (!_items.TryGetValue(id, out var list))
        {
            list = new List<object>();
            _items[id] = list;
        }
        list.Add(content);
        return Result.Success();
    }

    /// <summary>Регистрирует набор контента по ID.</summary>
    public Result RegisterAll(IEnumerable<KeyValuePair<string, object>> contents)
    {
        if (contents is null)
            return Result.Failure("Contents collection cannot be null.");

        var failures = new List<string>();
        foreach (var pair in contents)
        {
            var result = Register(pair.Key, pair.Value);
            if (result.IsFailure)
                failures.Add(result.Error!);
        }
        return failures.Count == 0 ? Result.Success() : Result.Failure(string.Join("; ", failures));
    }

    /// <summary>Возвращает контент по ID и типу.</summary>
    public Result<T> Get<T>(string id) where T : class
    {
        if (!_items.TryGetValue(id, out var list))
            return Result<T>.Failure($"Content '{id}' not found.");

        foreach (var item in list)
        {
            if (item is T typed)
                return Result<T>.Success(typed);
        }
        return Result<T>.Failure($"Content '{id}' has no item of type {typeof(T).Name}.");
    }

    /// <summary>Пытается получить контент по ID и типу.</summary>
    public bool TryGet<T>(string id, out T? content) where T : class
    {
        content = null;
        if (!_items.TryGetValue(id, out var list))
            return false;

        foreach (var item in list)
        {
            if (item is T typed)
            {
                content = typed;
                return true;
            }
        }
        return false;
    }

    public void Clear() => _items.Clear();
}
