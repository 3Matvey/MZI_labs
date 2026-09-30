using System.Text;
using ConsoleUi;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
new DemoApplication().Run();

internal sealed class DemoApplication
{
    private readonly Ui _ui = new();
    private readonly string _samples = Path.Combine(AppContext.BaseDirectory, "Samples");

    public void Run()
    {
        Console.WriteLine("ConsoleUi — демонстрация всех операций");
        Console.WriteLine("В списках: ↑↓, Enter; Esc отменяет выбор или ввод текста и чисел.");
        while (true)
        {
            var action = _ui.Select("Что попробовать?", CreateMenu());
            if (action.IsCancelled) return;
            action.Value();
            Console.WriteLine();
        }
    }

    private Choice<Action>[] CreateMenu() =>
    [
        new("Select<T> — выбор режима", SelectMode),
        new("PickFile — выбор файла", PickFile),
        new("PickDirectory — выбор папки", PickDirectory),
        new("PickSavePath — путь нового файла", PickSavePath),
        new("ReadText — строка с проверкой", ReadText),
        new("ReadNumber — число с проверкой", ReadNumber),
        new("Confirm — подтверждение", Confirm),
        new("ShowSuccess / ShowError — сообщения", ShowMessages)
    ];

    private void SelectMode() => Show(_ui.Select("Выберите режим", new Choice<Mode>[]
    {
        new("Простая замена (ECB)", Mode.Ecb),
        new("Гаммирование (CNT)", Mode.Cnt),
        new("Обратная связь (CFB)", Mode.Cfb)
    }));

    private void PickFile()
    {
        Console.WriteLine("Фильтр: только .txt. Для примера рядом с программой есть папка Samples.");
        Show(_ui.PickFile("Выберите текстовый файл", _samples,
            path => Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase)));
    }

    private void PickDirectory() => Show(_ui.PickDirectory("Выберите папку"));

    private void PickSavePath()
    {
        Console.WriteLine("Демо только выбирает путь — файл не создаётся.");
        Show(_ui.PickSavePath("Куда сохранить новый ключ?", "key.bin"));
    }

    private void ReadText() => Show(_ui.ReadText("Ваше имя", "Студент",
        value => value.Length >= 2 ? null : "Нужно хотя бы два символа."));

    private void ReadNumber() => Show(_ui.ReadNumber<int>("Количество повторений (1–100)", 10,
        value => value is >= 1 and <= 100 ? null : "Введите число от 1 до 100."));

    private void Confirm() => Show(_ui.Confirm("Продолжить демонстрационную операцию?"));

    private void ShowMessages()
    {
        _ui.ShowSuccess("Файл сохранён: пример.txt");
        _ui.ShowError("Демонстрационное сообщение: выбран неверный ключ.");
        _ui.ShowSuccess("Имена с [квадратными скобками] тоже отображаются правильно.");
    }

    private void Show<T>(PromptResult<T> result)
    {
        if (result.IsCancelled) Console.WriteLine("Действие отменено.");
        else _ui.ShowSuccess($"Получено значение: {result.Value}");
    }

    private enum Mode { Ecb, Cnt, Cfb }
}
