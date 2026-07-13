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
        AllowTrailingCommas = true
    };

    public Result<IEnumerable<T>> LoadActorsFromFile<T>(string filePath) where T : BaseActorTemplate =>
        LoadFromFile<T>(filePath, "actor");

    public Result<IEnumerable<T>> LoadActionsFromFile<T>(string filePath) where T : BaseActionTemplate =>
        LoadFromFile<T>(filePath, "action");

    public Result<IEnumerable<T>> LoadActorsFromDirectory<T>(string directoryPath) where T : BaseActorTemplate =>
        LoadFromDirectory<T>(directoryPath, "actor");

    public Result<IEnumerable<T>> LoadActionsFromDirectory<T>(string directoryPath) where T : BaseActionTemplate =>
        LoadFromDirectory<T>(directoryPath, "action");

    private Result<IEnumerable<T>> LoadFromFile<T>(string filePath, string typeSuffix)
    {
        try
        {
            if (!File.Exists(filePath))
                return Result<IEnumerable<T>>.Failure($"File not found: {filePath}");

            string json = File.ReadAllText(filePath);
            using JsonDocument doc = JsonDocument.Parse(json);
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

        List<T> results = [];
        List<string> errors = [];

        foreach (string file in Directory.EnumerateFiles(directoryPath, "*.json", SearchOption.AllDirectories))
        {
            Result<IEnumerable<T>> result = LoadFromFile<T>(file, typeSuffix);
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

        IEnumerable<JsonElement> elements = root.ValueKind == JsonValueKind.Array
            ? root.EnumerateArray()
            : [root];

        List<T> parsed = [];

        foreach (JsonElement element in elements)
        {
            if (!element.TryGetProperty("$type", out JsonElement typeProp))
                continue;

            string? discriminator = typeProp.GetString();
            if (string.IsNullOrWhiteSpace(discriminator) || !discriminator.EndsWith(typeSuffix))
                continue;

            T? deserialized = JsonSerializer.Deserialize<T>(element, _options);
            if (deserialized is not null)
                parsed.Add(deserialized);
        }

        return parsed.Count > 0
            ? Result<IEnumerable<T>>.Success(parsed)
            : Result<IEnumerable<T>>.Failure($"No elements matching '{typeSuffix}' found in JSON.");
    }
}