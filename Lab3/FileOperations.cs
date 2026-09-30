using ConsoleUi;

namespace MziLab;

internal static class FileOperations
{
    public static void Transform(string input, string key, string output, bool decrypt, Ui ui)
    {
        byte[] data = File.ReadAllBytes(input);
        byte[] result = decrypt
            ? RabinFile.Decrypt(data, KeyFiles.ReadPrivate(key))
            : RabinFile.Encrypt(data, KeyFiles.ReadPublic(key));
        using var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        file.Write(result);
        ui.ShowSuccess($"Сохранено {result.Length} байт: {output}");
    }

}
