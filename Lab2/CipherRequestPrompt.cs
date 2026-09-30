using ConsoleUi;

namespace MziLab;

internal sealed class CipherRequestPrompt(Ui ui)
{
    public CipherRequest? Read(string action)
    {
        var mode = ui.Select("Выберите режим", new Choice<string>[]
        {
            new("Простая замена (ECB)", "ecb"),
            new("Обратная связь (CFB)", "cfb")
        });
        return mode.IsCancelled ? null : ReadFiles(action, mode.Value);
    }

    private CipherRequest? ReadFiles(string action, string mode)
    {
        string title = action == "encrypt" ? "Выберите файл для шифрования" : "Выберите зашифрованный файл";
        var input = ui.PickFile(title);
        if (input.IsCancelled) return null;
        var key = ReadKey();
        if (key.IsCancelled) return null;
        var output = ui.PickSavePath("Куда сохранить результат?", SuggestName(input.Value, action), Path.GetDirectoryName(input.Value));
        if (output.IsCancelled) return null;
        return new CipherRequest(action, mode, input.Value, output.Value, key.Value);
    }

    private PromptResult<string> ReadKey()
    {
        while (true)
        {
            var key = ui.PickFile("Выберите файл ключа (32 байта)");
            if (key.IsCancelled || new FileInfo(key.Value).Length == 32) return key;
            ui.ShowError("Ключ BelT должен содержать ровно 32 байта. Выберите другой файл.");
        }
    }

    private static string SuggestName(string input, string action) => action == "encrypt"
        ? Path.GetFileName(input) + ".enc"
        : "restored_" + Path.GetFileNameWithoutExtension(input);
}

