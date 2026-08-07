namespace Sektor.TurnBased.Core.Abstractions;

/// <summary>
/// Результат операции без возвращаемого значения.
/// Сам по себе является булевым: IsSuccess или IsFailure.
/// Используется вместо Result&lt;bool&gt; и вместо исключений.
/// </summary>
public readonly struct Result
{
    private readonly string? _error;

    /// <summary>Ошибка или null, если операция успешна.</summary>
    public string? Error => _error;

    public bool IsSuccess => _error is null;
    public bool IsFailure => _error is not null;

    private Result(string? error) => _error = error;

    public static Result Success() => new(null);

    public static Result Failure(string error) => new(error);
}
