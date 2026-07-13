using Sektor.TurnBased.GameCore.Actions;
using Sektor.TurnBased.GameCore.Actors;
using Sektor.TurnBased.GameCore.Extensions;
using System.Text.Json;

namespace Sektor.TurnBased.GameCore.IO;

/// <summary>
/// Загрузчик шаблонов из JSON. Отвечает только за чтение файлов/потоков и десериализацию.
/// Не хранит данные. Не знает о репозиториях. Возвращает готовые объекты через Result.
/// </summary>
public sealed class TemplateJsonLoader
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        // Полиморфизм обрабатывается нативно через [JsonDerivedType] в базовых классах
    };

    /// <summary>Загружает актёров из одного JSON-файла.</summary>
    public Result<IEnumerable<T>> LoadActorsFromFile<T>(string filePath) where T : BaseActorTemplate
    {
        return LoadFromFile<T>(filePath, "actor");
    }

    /// <summary>Загружает действия из одного JSON-файла.</summary>
    public Result<IEnumerable<T>> LoadActionsFromFile<T>(string filePath) where T : BaseActionTemplate
    {
        return LoadFromFile<T>(filePath, "action");
    }

    /// <summary>Рекурсивно загружает актёров из папки (*.json).</summary>
    public Result<IEnumerable<T>> LoadActorsFromDirectory<T>(string directoryPath) where T : BaseActorTemplate
    {
        return LoadFromDirectory<T>(directoryPath, "actor");
    }

    /// <summary>Рекурсивно загружает действия из папки (*.json).</summary>
    public Result<IEnumerable<T>> LoadActionsFromDirectory<T>(string directoryPath) where T : BaseActionTemplate
    {
        return LoadFromDirectory<T>(directoryPath, "action");
    }

    private Result<IEnumerable<T>> LoadFromFile<T>(string filePath, string typeSuffix)
    {
        try
        {
            if (!File.Exists(filePath))
                return Result<IEnumerable<T>>.Failure($"File not found: {filePath}");

            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            return ParseElements<T>(doc.RootElement, typeSuffix);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<T>>.Failure($"Error loading file '{filePath}': {ex.Message}");
        }
    }

    private Result<IEnumerable<T>> LoadFromDirectory<T>(string directoryPath, string typeSuffix)
    {
        if (!Directory.Exists(directoryPath))
            return Result<IEnumerable<T>>.Failure($"Directory not found: {directoryPath}");

        var results = new List<T>();
        var errors = new List<string>();

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.json", SearchOption.AllDirectories))
        {
            var result = LoadFromFile<T>(file, typeSuffix);
            if (result.IsSuccess)
                results.AddRange(result.Value);
            else
                errors.Add(result.Error);
        }

        return results.Count > 0
            ? Result<IEnumerable<T>>.Success(results)
            : Result<IEnumerable<T>>.Failure($"No valid templates loaded. Errors: {string.Join(", ", errors)}");
    }

    private Result<IEnumerable<T>> ParseElements<T>(JsonElement root, string typeSuffix)
    {
        if (root.ValueKind != JsonValueKind.Array && root.ValueKind != JsonValueKind.Object)
            return Result<IEnumerable<T>>.Failure("JSON root must be an array or object.");

        var elements = root.ValueKind == JsonValueKind.Array
            ? root.EnumerateArray()
            : new[] { root }.AsEnumerable();

        var parsed = new List<T>();

        foreach (var element in elements)
        {
            // Фильтр по суффиксу дискриминатора предотвращает смешивание категорий
            if (!element.TryGetProperty("$type", out var typeProp))
                continue;

            var discriminator = typeProp.GetString();
            if (string.IsNullOrWhiteSpace(discriminator) || !discriminator.EndsWith(typeSuffix))
                continue;

            // STJ автоматически создаст правильный дочерний тип (DgActor, StsAction и т.д.)
            var deserialized = JsonSerializer.Deserialize<T>(element, _options);
            if (deserialized is not null)
                parsed.Add(deserialized);
        }

        return parsed.Count > 0
            ? Result<IEnumerable<T>>.Success(parsed)
            : Result<IEnumerable<T>>.Failure($"No elements matching '{typeSuffix}' found in JSON.");
    }
}