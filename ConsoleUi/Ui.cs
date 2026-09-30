using Spectre.Console;

namespace ConsoleUi;

/// <summary>Меню, ввод значений и выбор файлов для консольных приложений.</summary>
/// <remarks>
/// Используйте один экземпляр на приложение, чтобы сохранять последнюю открытую папку.
/// Методы вызываются последовательно: одновременные диалоги из разных потоков не поддерживаются.
/// Отмена запроса возвращает <see cref="PromptResult{T}.IsCancelled"/>, а не ошибку.
/// </remarks>
public sealed partial class Ui
{
    private readonly IAnsiConsole _console;
    private readonly TextReader _input;
    private readonly bool _interactive;
    private readonly FilePicker _files;

    /// <summary>Создаёт интерфейс для терминала или построчного ввода.</summary>
    /// <param name="console">Консоль Spectre.Console; при <see langword="null"/> используется стандартная консоль.</param>
    /// <param name="input">
    /// Необязательный источник строк, например StringReader для тестов.
    /// Если передан, всегда используется построчный режим без чтения клавиш.
    /// Если не передан, используется Console.In, а режим определяется возможностями консоли.
    /// </param>
    /// <remarks>
    /// В интерактивном режиме отмена выполняется Esc. В построчном режиме
    /// списки отменяются числом 0, текстовые поля — строкой /cancel; EOF также отменяет запрос.
    /// </remarks>
    public Ui(IAnsiConsole? console = null, TextReader? input = null)
    {
        _console = console ?? AnsiConsole.Console;
        _input = input ?? Console.In;
        _interactive = input is null && _console.Profile.Capabilities.Interactive;
        _files = new FilePicker(this);
    }

    /// <summary>Открывает навигатор для выбора существующего файла.</summary>
    /// <param name="title">Заголовок запроса, выводимый без интерпретации разметки.</param>
    /// <param name="initialDirectory">
    /// Начальная папка. При null используется последняя открытая папка, при первом вызове — рабочая папка приложения.
    /// Относительный путь разрешается относительно последней открытой папки.
    /// </param>
    /// <param name="filter">
    /// Необязательная проверка полного пути: true разрешает выбор файла, false скрывает его.
    /// Применяется и к ручному вводу пути; папки не фильтруются.
    /// </param>
    /// <returns>Полный путь файла или отмена. Недоступная начальная папка вызывает сообщение и отмену.</returns>
    /// <remarks>
    /// Поддерживает переходы между папками и дисками, а также ручной ввод пути в кавычках.
    /// Файл не открывается: при последующем чтении приложение само обрабатывает ошибки доступа или удаления.
    /// </remarks>
    public PromptResult<string> PickFile(string title, string? initialDirectory = null,
        Func<string, bool>? filter = null) => _files.Pick(title, initialDirectory, false, filter);

    /// <summary>Открывает навигатор для выбора существующей папки.</summary>
    /// <param name="title">Заголовок запроса.</param>
    /// <param name="initialDirectory">
    /// Начальная папка или null для последней открытой папки (первоначально — рабочей папки приложения).
    /// Относительные пути отсчитываются от последней открытой папки.
    /// </param>
    /// <returns>Полный путь папки или отмена, в том числе если начальная папка недоступна.</returns>
    /// <remarks>
    /// Для подтверждения нажмите «Выбрать эту папку». Ручной ввод пути сначала открывает папку.
    /// Выбор не создаёт папку и не проверяет возможность записи в неё.
    /// </remarks>
    public PromptResult<string> PickDirectory(string title, string? initialDirectory = null) =>
        _files.Pick(title, initialDirectory, true);

    /// <summary>Предлагает варианты «Да» и «Нет» с возможностью отмены.</summary>
    /// <param name="title">Вопрос пользователю.</param>
    /// <returns>Успешный результат с true для «Да», false для «Нет» либо отмена при Esc/выборе отмены.</returns>
    /// <remarks>Сначала проверяйте IsCancelled: ответ «Нет» и отмена — разные результаты.</remarks>
    public PromptResult<bool> Confirm(string title) =>
        Select(title, [new Choice<bool>("Да", true), new Choice<bool>("Нет", false)]);

    /// <summary>Выводит сообщение с зелёной подписью «Успешно» и переводом строки.</summary>
    /// <param name="message">Обычный текст; квадратные скобки не интерпретируются как разметка.</param>
    /// <remarks>Если терминал не поддерживает цвет, смысл сообщения сохраняется в подписи.</remarks>
    public void ShowSuccess(string message) => WriteStatus("Успешно", message, Color.Green);

    /// <summary>Выводит сообщение с красной подписью «Ошибка» и переводом строки.</summary>
    /// <param name="message">Обычный текст; квадратные скобки не интерпретируются как разметка.</param>
    /// <remarks>Только выводит сообщение в настроенную консоль: не бросает исключение и не завершает операцию.</remarks>
    public void ShowError(string message) => WriteStatus("Ошибка", message, Color.Red);

    private void WriteStatus(string label, string message, Color color)
    {
        _console.Write(new Text($"{label}: ", new Style(color)));
        _console.WriteLine(message);
    }

    internal void WriteLine(string message) => _console.WriteLine(message);

    internal string FormatPath(string path)
    {
        int width = Math.Max(12, _console.Profile.Width - 2);
        return path.Length <= width ? path : "…" + path[^(width - 1)..];
    }
}
