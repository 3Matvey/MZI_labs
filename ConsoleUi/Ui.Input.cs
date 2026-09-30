using System.Globalization;
using System.Numerics;
using Spectre.Console;

namespace ConsoleUi;

public sealed partial class Ui
{
    /// <summary>Запрашивает непустую строку, повторяя ввод при ошибке проверки.</summary>
    /// <param name="title">Подпись поля, выводимая как обычный текст.</param>
    /// <param name="defaultValue">
    /// Значение, принимаемое при пустом вводе (Enter). При null значения по умолчанию нет.
    /// Значение по умолчанию проходит те же проверки, что и введённое вручную.
    /// </param>
    /// <param name="validate">
    /// Необязательный валидатор: null означает корректное значение, строка — сообщение об ошибке.
    /// Вызывается только для строки, которая не пуста и не состоит из одних пробелов.
    /// Исключения валидатора не перехватываются.
    /// </param>
    /// <returns>Проверенная строка или отмена. Пробелы по краям непустого значения сохраняются.</returns>
    /// <remarks>
    /// В интерактивном режиме Esc немедленно отменяет ввод, включая частично набранное значение;
    /// строка /cancel здесь является обычным текстом.
    /// Только в построчном режиме точная строка /cancel означает отмену, поскольку события клавиш недоступны.
    /// Конец входного потока также отменяет запрос. Ошибка проверки выводится перед повторным вводом.
    /// </remarks>
    public PromptResult<string> ReadText(string title, string? defaultValue = null,
        Func<string, string?>? validate = null)
    {
        while (true)
        {
            string? line = ReadLine(title, defaultValue);
            if (line is null || (!_interactive && line == "/cancel")) return PromptResult<string>.Cancelled;
            string value = line.Length == 0 ? defaultValue ?? "" : line;
            string? error = string.IsNullOrWhiteSpace(value) ? "Введите непустое значение." : validate?.Invoke(value);
            if (error is null) return PromptResult<string>.Success(value);
            ShowError(error);
        }
    }

    /// <summary>Запрашивает конечное число заданного типа и повторяет ввод при ошибке.</summary>
    /// <typeparam name="T">Числовой тип-структура, реализующий INumber, например int, double или decimal.</typeparam>
    /// <param name="title">Подпись числового поля.</param>
    /// <param name="defaultValue">Число, принимаемое по Enter; null отключает значение по умолчанию.</param>
    /// <param name="validate">
    /// Дополнительная проверка числа: null — успех, строка — ошибка, например нарушение диапазона.
    /// Вызывается после разбора числа; исключения валидатора передаются вызывающему коду.
    /// </param>
    /// <returns>Проверенное число или отмена. Число 0 само по себе отменой не является.</returns>
    /// <remarks>
    /// Используется CultureInfo.CurrentCulture, включая её десятичный разделитель.
    /// Неверный формат, переполнение, NaN и бесконечность отклоняются.
    /// Отмена работает как в ReadText: Esc интерактивно, /cancel построчно, EOF в обоих режимах.
    /// </remarks>
    public PromptResult<T> ReadNumber<T>(string title, T? defaultValue = null,
        Func<T, string?>? validate = null) where T : struct, INumber<T>
    {
        var text = ReadText(title, defaultValue?.ToString(), value => ValidateNumber(value, validate));
        return text.IsCancelled ? PromptResult<T>.Cancelled
            : PromptResult<T>.Success(T.Parse(text.Value, CultureInfo.CurrentCulture));
    }

    private static string? ValidateNumber<T>(string text, Func<T, string?>? validate)
        where T : struct, INumber<T>
    {
        if (!T.TryParse(text, CultureInfo.CurrentCulture, out T value) || !T.IsFinite(value))
        {
            return "Введите корректное конечное число в формате текущей локали.";
        }
        return validate?.Invoke(value);
    }

    private string? ReadLine(string title, string? defaultValue)
    {
        string label = defaultValue is null ? title : $"{title} [{defaultValue}]";
        label += _interactive ? " (Esc — отмена):" : " (/cancel — отмена):";
        if (_interactive)
        {
            return ReadInteractiveLine(label);
        }
        WriteLine(label);
        return _input.ReadLine();
    }

    private string? ReadInteractiveLine(string label)
    {
        try
        {
            return new EscapablePromptConsole(_console)
                .Prompt(new TextPrompt<string>(Markup.Escape(label)).AllowEmpty());
        }
        catch (EscapablePromptConsole.PromptCancelledException)
        {
            WriteLine("");
            return null;
        }
    }

    /// <summary>Предлагает выбрать папку и имя нового файла, не создавая сам файл.</summary>
    /// <param name="title">Заголовок выбора папки сохранения.</param>
    /// <param name="suggestedName">Имя файла без пути, принимаемое по Enter, например key.bin.</param>
    /// <param name="initialDirectory">Начальная папка или null для последней открытой папки.</param>
    /// <returns>Полный путь нового файла или отмена на любом из двух шагов.</returns>
    /// <remarks>
    /// Отклоняет пустое имя, недопустимые символы, путь вместо имени и занятые имена файлов/папок.
    /// Проверяет специальные ограничения имён Windows. Ошибка имени приводит к повторному вводу.
    /// Не создаёт файл, не резервирует путь и не проверяет права записи.
    /// Приложение должно открывать результат через FileMode.CreateNew и обрабатывать ошибки записи:
    /// после выбора путь может оказаться занят или папка может исчезнуть.
    /// </remarks>
    public PromptResult<string> PickSavePath(string title, string suggestedName,
        string? initialDirectory = null)
    {
        var folder = PickDirectory(title, initialDirectory);
        if (folder.IsCancelled) return PromptResult<string>.Cancelled;
        var name = ReadText("Имя нового файла", suggestedName,
            value => FilePaths.ValidateNewFile(folder.Value, value));
        return name.IsCancelled ? PromptResult<string>.Cancelled
            : PromptResult<string>.Success(Path.Combine(folder.Value, name.Value));
    }
}
