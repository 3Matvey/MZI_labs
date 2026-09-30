using ConsoleUi;
using System.Security.Cryptography;

namespace MziLab;

internal sealed class ConsoleMenu
{
    private readonly Ui _ui = new();
    private readonly CipherRequestPrompt _requests;

    public ConsoleMenu()
    {
        _requests = new CipherRequestPrompt(_ui);
    }

    public void Run()
    {
        Console.WriteLine("Лабораторная № 2 — BelT (СТБ 34.101.31-2011)");
        Console.WriteLine("Esc — отменить действие или выйти из главного меню.");
        while (true)
        {
            var selected = _ui.Select("Выберите действие", CreateMenu());
            if (selected.IsCancelled || selected.Value is null) return;
            Execute(selected.Value);
        }
    }

    private Choice<Action?>[] CreateMenu() =>
    [
        new("Создать ключ", GenerateKey),
        new("Зашифровать файл", () => Transform("encrypt")),
        new("Расшифровать файл", () => Transform("decrypt")),
        new("Выход", null)
    ];

    private void Execute(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is ArgumentException or IOException
            or UnauthorizedAccessException or FormatException
            or CryptographicException or InvalidOperationException)
        {
            _ui.ShowError(exception.Message);
        }
    }

    private void GenerateKey()
    {
        var path = _ui.PickSavePath("Куда сохранить новый ключ (256 бит)?", "key.bin");
        if (path.HasValue) FileOperations.GenerateKey(path.Value, _ui);
    }

    private void Transform(string action)
    {
        var request = _requests.Read(action);
        if (request is not null) CipherService.Execute(request, _ui);
    }

}
