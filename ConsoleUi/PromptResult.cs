namespace ConsoleUi;

/// <summary>Результат запроса: выбранное значение либо отмена пользователем.</summary>
/// <typeparam name="T">Тип возвращаемого значения.</typeparam>
/// <remarks>default(PromptResult&lt;T&gt;) означает отмену. Успешные false, 0 и null не означают отмену.</remarks>
public readonly struct PromptResult<T>
{
    private readonly T? _value;
    /// <summary>True, если запрос вернул значение, в том числе default(T).</summary>
    public bool HasValue { get; }
    /// <summary>True, если запрос отменён; обращение к Value в таком состоянии запрещено.</summary>
    public bool IsCancelled => !HasValue;
    /// <summary>Выбранное значение. Перед чтением проверьте HasValue или IsCancelled.</summary>
    /// <exception cref="InvalidOperationException">Запрос отменён и значения нет.</exception>
    public T Value => HasValue ? _value! : throw new InvalidOperationException("Выбор отменён.");

    private PromptResult(T value)
    {
        _value = value;
        HasValue = true;
    }

    /// <summary>Создаёт результат отмены без значения; это штатный исход, а не ошибка.</summary>
    public static PromptResult<T> Cancelled => default;
    /// <summary>Создаёт успешный результат с переданным значением.</summary>
    /// <param name="value">Значение без дополнительной проверки, включая default(T).</param>
    /// <returns>Результат с HasValue = true.</returns>
    public static PromptResult<T> Success(T value) => new(value);
}
