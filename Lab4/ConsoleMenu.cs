using ConsoleUi;
using System.Security.Cryptography;

namespace MziLab;

internal sealed class ConsoleMenu
{
    private readonly Ui _ui = new();

    public void Run()
    {
        Console.WriteLine("Лабораторная № 4 — криптосистема Мак-Элиса");
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
        new("Создать пару ключей", GenerateKeys),
        new("Зашифровать файл", () => Transform(false)),
        new("Расшифровать файл", () => Transform(true)),
        new("Выход", null)
    ];

    private void Execute(Action action)
    {
        try { action(); }
        catch (Exception error) when (error is ArgumentException or IOException
            or UnauthorizedAccessException or CryptographicException
            or InvalidOperationException or OverflowException)
        {
            _ui.ShowError(error.Message);
        }
    }

    private void GenerateKeys()
    {
        var folder = _ui.PickDirectory("Куда сохранить public-key.bin и private-key.bin?");
        if (folder.IsCancelled) return;
        if (File.Exists(Path.Combine(folder.Value, "public-key.bin"))
            || File.Exists(Path.Combine(folder.Value, "private-key.bin")))
            throw new IOException("В папке уже есть ключи. Выберите другую папку.");
        Console.WriteLine("Генерация матриц и кода Гоппы, подождите…");
        McElieceKeys.Generate(folder.Value);
        _ui.ShowSuccess($"Ключи сохранены в {folder.Value}");
    }

    private void Transform(bool decrypt)
    {
        var input = _ui.PickFile(decrypt ? "Выберите шифрофайл" : "Выберите исходный файл");
        if (input.IsCancelled) return;
        var key = _ui.PickFile(decrypt ? "Выберите private-key.bin" : "Выберите public-key.bin");
        if (key.IsCancelled) return;
        string name = decrypt ? "restored_" + Path.GetFileNameWithoutExtension(input.Value)
            : Path.GetFileName(input.Value) + ".mce";
        var output = _ui.PickSavePath("Куда сохранить результат?", name, Path.GetDirectoryName(input.Value));
        if (output.IsCancelled) return;
        McElieceFiles.Transform(input.Value, key.Value, output.Value, decrypt);
        _ui.ShowSuccess($"Результат сохранён: {output.Value}");
    }
}
