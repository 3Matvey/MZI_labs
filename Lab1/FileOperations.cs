using ConsoleUi;
using System.Security.Cryptography;

namespace MziLab;

internal static class FileOperations
{
    public static void GenerateKey(string path, Ui ui)
    {
        SaveNew(path, RandomNumberGenerator.GetBytes(32));
        ui.ShowSuccess($"Ключ сохранён: {Path.GetFullPath(path)}");
    }

    public static void SaveResult(string path, byte[] data, Ui ui)
    {
        SaveNew(path, data);
        ui.ShowSuccess($"Сохранено {data.Length} байт: {Path.GetFullPath(path)}");
    }

    private static void SaveNew(string path, byte[] data)
    {
        //                                             CreateNew не позволяет перезаписать ни исходник, ни ключ, ни другой файл
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        file.Write(data);
    }
}
