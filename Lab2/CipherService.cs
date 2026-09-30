using ConsoleUi;
using System.Security.Cryptography;

namespace MziLab;

internal static class CipherService
{
    public static void Execute(CipherRequest request, Ui ui)
    {
        var cipher = new Belt(File.ReadAllBytes(request.KeyPath));
        byte[] data = File.ReadAllBytes(request.Input);
        byte[] result = Transform(cipher, data, request);
        FileOperations.SaveResult(request.Output, result, ui);
    }

    private static byte[] Transform(Belt cipher, byte[] data, CipherRequest request) =>
        request.Action switch
        {
            "encrypt" when request.Mode == "ecb" => cipher.EcbEncrypt(data),
            "decrypt" when request.Mode == "ecb" => cipher.EcbDecrypt(data),
            "encrypt" => EncryptStream(cipher, data),
            _ => DecryptStream(cipher, data)
        };

    private static byte[] EncryptStream(Belt cipher, byte[] data)
    {
        byte[] iv = RandomNumberGenerator.GetBytes(cipher.BlockSize);
        return [.. iv, .. cipher.Cfb(data, iv)];
    }

    private static byte[] DecryptStream(Belt cipher, byte[] data)
    {
        if (data.Length < cipher.BlockSize)
        {
            throw new ArgumentException("В файле отсутствует полная синхропосылка (IV).");
        }
        byte[] iv = data[..cipher.BlockSize];
        return cipher.Cfb(data[cipher.BlockSize..], iv, decrypt: true);
    }
}


