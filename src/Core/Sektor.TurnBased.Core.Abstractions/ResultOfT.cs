using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sektor.TurnBased.Core.Abstractions;

/// <summary>
/// Результат операции с возвращаемым значением.
/// Канонический способ передачи «значение или ошибка» без исключений.
/// </summary>
public readonly struct Result<T>
{
    private readonly bool _isSuccess;
    private readonly T? _value;
    private readonly string? _error;

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;
    public T? Value => _value;
    public string? Error => IsFailure ? _error : null;

    private Result(bool isSuccess, T? value, string? error)
    {
        _isSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Success(T value) => new(true, value, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Failure(string error) => new(false, default, error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value!;
        return _isSuccess;
    }
}
