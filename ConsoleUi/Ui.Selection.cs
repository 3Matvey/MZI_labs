using Spectre.Console;

namespace ConsoleUi;

public sealed partial class Ui
{
    /// <summary>Предлагает выбрать одно типизированное значение из списка.</summary>
    /// <typeparam name="T">Тип значений пунктов; например enum, string или Action.</typeparam>
    /// <param name="title">Заголовок списка, выводимый без интерпретации разметки.</param>
    /// <param name="choices">Непустая последовательность пунктов с подписью и значением. Перечисляется один раз.</param>
    /// <returns>Значение выбранного пункта или отмена, отличимая от default(T).</returns>
    /// <exception cref="ArgumentNullException">Вместо choices передан null.</exception>
    /// <exception cref="ArgumentException">Последовательность choices пуста.</exception>
    /// <remarks>
    /// Интерактивно: стрелки меняют выбор, Enter подтверждает, Esc отменяет; ввод текста ищет по подписям.
    /// В построчном режиме вводится номер от 1 до числа пунктов; 0, /cancel и EOF отменяют запрос.
    /// Некорректный номер вызывает повторный ввод. Пункт «Отмена / назад» добавляется автоматически.
    /// </remarks>
    public PromptResult<T> Select<T>(string title, IEnumerable<Choice<T>> choices)
    {
        ArgumentNullException.ThrowIfNull(choices);
        var items = choices.ToArray();
        if (items.Length == 0)
        {
            throw new ArgumentException("Список вариантов не может быть пустым.", nameof(choices));
        }
        return _interactive ? SelectInteractive(title, items) : SelectNumbered(title, items);
    }

    private PromptResult<T> SelectInteractive<T>(string title, Choice<T>[] items)
    {
        var choices = items.Select(item => new Selection<T>(item.Label, PromptResult<T>.Success(item.Value)));
        var cancel = new Selection<T>("Отмена / назад", PromptResult<T>.Cancelled);
        var prompt = CreateSelectionPrompt(title, choices, cancel);
        return _console.Prompt(prompt).Result;
    }

    private static SelectionPrompt<Selection<T>> CreateSelectionPrompt<T>(string title,
        IEnumerable<Selection<T>> choices, Selection<T> cancel) =>
        new SelectionPrompt<Selection<T>>()
            .Title(Markup.Escape(title)).PageSize(12).EnableSearch()
            .SearchPlaceholderText("↑↓ — выбор, Enter — открыть, Esc — отмена; печатайте для поиска")
            .MoreChoicesText("↑↓ — прокрутка списка")
            .HighlightStyle(new Style(Color.Black, Color.Cyan1))
            .UseConverter(item => Markup.Escape(item.Label))
            .AddChoices(choices).AddChoices(cancel).AddCancelResult(cancel);

    private PromptResult<T> SelectNumbered<T>(string title, Choice<T>[] items)
    {
        PrintNumberedChoices(title, items);
        while (true)
        {
            string? line = _input.ReadLine();
            if (line is null || line.Trim() is "0" or "/cancel") return PromptResult<T>.Cancelled;
            if (int.TryParse(line, out int number) && number >= 1 && number <= items.Length)
            {
                return PromptResult<T>.Success(items[number - 1].Value);
            }
            ShowError($"Введите число от 1 до {items.Length} или 0 для отмены.");
        }
    }

    private void PrintNumberedChoices<T>(string title, Choice<T>[] items)
    {
        WriteLine(title);
        for (int index = 0; index < items.Length; index++)
        {
            WriteLine($"{index + 1}. {items[index].Label}");
        }
        WriteLine("0. Отмена / назад");
    }

    private sealed record Selection<T>(string Label, PromptResult<T> Result);
}
