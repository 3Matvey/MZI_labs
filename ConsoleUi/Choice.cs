namespace ConsoleUi;

/// <summary>Пункт списка: отдельная подпись для пользователя и значение для приложения.</summary>
/// <typeparam name="T">Тип значения, возвращаемого при выборе пункта.</typeparam>
/// <param name="Label">Отображаемая подпись. Разметка Spectre.Console не интерпретируется.</param>
/// <param name="Value">Значение пункта, например элемент enum; подпись не используется для определения результата.</param>
public sealed record Choice<T>(string Label, T Value);
