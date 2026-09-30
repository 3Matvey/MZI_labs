using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;

namespace MziLab;

internal static class KeyFiles
{
    public static (BigInteger P, BigInteger Q) Generate()
    {
        BigInteger p = PrimeNumbers.Generate();
        BigInteger q;
        do { q = PrimeNumbers.Generate(); } while (p == q);
        return (p, q);
    }

    public static void Save(string directory, BigInteger p, BigInteger q)
    {
        string publicPath = Path.Combine(directory, "public-key.json");
        string privatePath = Path.Combine(directory, "private-key.json");
        using var publicFile = new FileStream(publicPath, FileMode.CreateNew, FileAccess.Write);
        using var privateFile = new FileStream(privatePath, FileMode.CreateNew, FileAccess.Write);
        JsonSerializer.Serialize(publicFile, new PublicKey(Hex(p * q)));
        JsonSerializer.Serialize(privateFile, new PrivateKey(Hex(p), Hex(q)));
    }

    public static BigInteger ReadPublic(string path)
    {
        var key = JsonSerializer.Deserialize<PublicKey>(File.ReadAllText(path));
        BigInteger modulus = Parse(key?.N);
        if (modulus.GetBitLength() != 2048 || modulus % 4 != 1)
            throw new CryptographicException("Нужен открытый ключ Рабина на 2048 бит.");
        return modulus;
    }

    public static RabinCipher ReadPrivate(string path)
    {
        var key = JsonSerializer.Deserialize<PrivateKey>(File.ReadAllText(path));
        BigInteger p = Parse(key?.P);
        BigInteger q = Parse(key?.Q);
        if (p.GetBitLength() != 1024 || q.GetBitLength() != 1024 || p == q
            || p % 4 != 3 || q % 4 != 3 || (p * q).GetBitLength() != 2048)
            throw new CryptographicException("Некорректные параметры закрытого ключа.");
        if (!PrimeNumbers.IsPrime(p) || !PrimeNumbers.IsPrime(q))
            throw new CryptographicException("Параметры закрытого ключа должны быть простыми числами.");
        return new RabinCipher(p, q);
    }

    private static BigInteger Parse(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || hex.Length > 512)
            throw new CryptographicException("Выбран не тот файл ключа или файл повреждён.");
        return new BigInteger(Convert.FromHexString(hex), isUnsigned: true, isBigEndian: true);
    }

    private static string Hex(BigInteger value) => Convert.ToHexString(value.ToByteArray(isUnsigned: true, isBigEndian: true));
    private sealed record PublicKey(string N);
    private sealed record PrivateKey(string P, string Q);
}
