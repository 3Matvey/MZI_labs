using ConsoleUi;
using System.Security.Cryptography;

namespace MziLab;

internal static class CipherService
{
    public static void Execute(CipherRequest request, Ui ui)
    {
        var cipher = new Gost28147(File.ReadAllBytes(request.KeyPath));
        byte[] data = File.ReadAllBytes(request.Input);
        if (request.Action == "verify")
        {
            Verify(cipher, data, request.Output, ui);
            return;
        }
        byte[] result = Transform(cipher, data, request);
        FileOperations.SaveResult(request.Output, result, ui);
        if (request.Action == "mac") ui.ShowSuccess($"Имитовставка: {Convert.ToHexString(result)}");
    }

    private static byte[] Transform(Gost28147 cipher, byte[] data, CipherRequest request) =>
        request.Action switch
        {
            "mac" => cipher.Mac(data),
            "encrypt" when request.Mode == "ecb" => cipher.EcbEncrypt(data),
            "decrypt" when request.Mode == "ecb" => cipher.EcbDecrypt(data),
            "encrypt" => EncryptStream(cipher, data, request),
            _ => DecryptStream(cipher, data, request.Mode)
        };

    private static byte[] EncryptStream(Gost28147 cipher, byte[] data, CipherRequest request)
    {
        byte[] iv = RandomNumberGenerator.GetBytes(cipher.BlockSize);
        byte[] payload = request.Mode == "cnt" ? cipher.Cnt(data, iv) : cipher.Cfb(data, iv);
        return [.. iv, .. payload];
    }

    private static byte[] DecryptStream(Gost28147 cipher, byte[] data, string mode)
    {
        if (data.Length < cipher.BlockSize)
        {
            throw new ArgumentException("В файле отсутствует полная синхропосылка (IV).");
        }
        byte[] iv = data[..cipher.BlockSize];
        byte[] payload = data[cipher.BlockSize..];
        return mode == "cnt" ? cipher.Cnt(payload, iv) : cipher.Cfb(payload, iv, decrypt: true);
    }

    private static void Verify(Gost28147 cipher, byte[] data, string path, Ui ui)
    {
        byte[] tag = File.ReadAllBytes(path);
        if (tag.Length != 4)
        {
            throw new ArgumentException("Файл имитовставки должен содержать ровно 4 байта.");
        }
        bool valid = cipher.VerifyMac(data, tag);
        if (valid) ui.ShowSuccess("Имитовставка совпадает.");
        else ui.ShowError("Имитовставка НЕ совпадает.");
    }
}


