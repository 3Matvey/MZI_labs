using ConsoleUi;

namespace MziLab;

internal sealed class CipherRequestPrompt(Ui ui)
{
    public CipherRequest? Read(string action)
    {
        if (action is "mac" or "verify") return ReadFiles(action, "imit");
        var mode = ReadMode();
        if (mode.IsCancelled) return null;
        return ReadFiles(action, mode.Value);
    }

    private CipherRequest? ReadFiles(string action, string mode)
    {
        var input = ui.PickFile(InputTitle(action));
        if (input.IsCancelled) return null;
        var key = ReadKey();
        if (key.IsCancelled) return null;
        var output = ReadOutput(action, input.Value);
        if (output.IsCancelled) return null;
        return new CipherRequest(action, mode, input.Value, output.Value, key.Value);
    }

    private PromptResult<string> ReadMode() => ui.Select("Выберите режим", new Choice<string>[]
    {
        new("Простая замена (ECB)", "ecb"),
        new("Гаммирование (CNT)", "cnt"),
        new("Гаммирование с обратной связью (CFB)", "cfb")
    });

    private PromptResult<string> ReadKey()
    {
        while (true)
        {
            var key = ui.PickFile("Выберите файл ключа (32 байта)");
            if (key.IsCancelled || new FileInfo(key.Value).Length == 32) return key;
            ui.ShowError("Ключ ГОСТ должен содержать ровно 32 байта. Выберите другой файл.");
        }
    }

    private PromptResult<string> ReadOutput(string action, string input)
    {
        return action == "verify"
            ? ui.PickFile("Выберите файл имитовставки (4 байта)")
            : ui.PickSavePath("Куда сохранить результат?", SuggestName(input, action), Path.GetDirectoryName(input));
    }

    private static string InputTitle(string action) => action switch
    {
        "encrypt" => "Выберите файл для шифрования",
        "decrypt" => "Выберите зашифрованный файл",
        _ => "Выберите файл, для которого вычисляется имитовставка"
    };

    private static string SuggestName(string input, string action) => action switch
    {
        "encrypt" => Path.GetFileName(input) + ".enc",
        "mac" => Path.GetFileName(input) + ".tag",
        _ => "restored_" + Path.GetFileNameWithoutExtension(input)
    };
}

