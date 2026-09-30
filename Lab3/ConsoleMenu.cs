using ConsoleUi;
using System.Security.Cryptography;
using System.Text.Json;

namespace MziLab;

internal sealed class ConsoleMenu
{
    private readonly Ui _ui = new();

    public void Run()
    {
        Console.WriteLine("Лабораторная № 3 — криптосистема Рабина");
        Console.WriteLine("Стрелки — выбор, Enter — подтверждение, Esc — отмена.");
        while (true)
        {
            var choice = _ui.Select("Выберите действие", CreateMenu());
            if (choice.IsCancelled || choice.Value is null) return;
            Execute(choice.Value);
        }
    }

    private Choice<Action?>[] CreateMenu() =>
    [
        new("Создать пару ключей (2048 бит)", GenerateKeys),
        new("Зашифровать файл открытым ключом", () => Transform(false)),
        new("Расшифровать файл закрытым ключом", () => Transform(true)),
        new("Выход", null)
    ];

    private void Execute(Action action)
    {
        try { action(); }
        catch (Exception exception) when (exception is ArgumentException or IOException
            or UnauthorizedAccessException or FormatException or CryptographicException
            or JsonException or InvalidOperationException or OverflowException)
        {
            _ui.ShowError(exception.Message);
        }
    }

    private void GenerateKeys()
    {
        var directory = _ui.PickDirectory("Куда сохранить public-key.json и private-key.json?");
        if (directory.IsCancelled) return;
        if (File.Exists(Path.Combine(directory.Value, "public-key.json"))
            || File.Exists(Path.Combine(directory.Value, "private-key.json")))
            throw new IOException("В папке уже есть ключи. Выберите другую папку.");
        Console.WriteLine("Генерация двух простых чисел, подождите…");
        var (p, q) = KeyFiles.Generate();
        KeyFiles.Save(directory.Value, p, q);
        _ui.ShowSuccess($"Пара ключей сохранена в {directory.Value}");
    }

    private void Transform(bool decrypt)
    {
        var input = _ui.PickFile(decrypt ? "Выберите зашифрованный файл" : "Выберите файл для шифрования");
        if (input.IsCancelled) return;
        var key = _ui.PickFile(decrypt ? "Выберите private-key.json (закрытый ключ)" : "Выберите public-key.json (открытый ключ)");
        if (key.IsCancelled) return;
        string name = decrypt ? "restored_" + Path.GetFileNameWithoutExtension(input.Value) : Path.GetFileName(input.Value);// + ".rabin";
        var output = _ui.PickSavePath("Куда сохранить результат?", name, Path.GetDirectoryName(input.Value));
        if (output.HasValue) FileOperations.Transform(input.Value, key.Value, output.Value, decrypt, _ui);
    }

}
