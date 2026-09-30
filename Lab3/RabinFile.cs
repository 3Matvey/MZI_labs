using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace MziLab;

internal static class RabinFile
{
    private const int HeaderSize = 28;
    private const int CipherBlockSize = 256;

    public static byte[] Encrypt(byte[] data, BigInteger modulus)
    {
        byte[] fileId = RandomNumberGenerator.GetBytes(16);
        byte[] result = new byte[checked(HeaderSize + BlockCount(data.Length) * CipherBlockSize)];
        "RABIN03\0"u8.CopyTo(result);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), data.Length);
        fileId.CopyTo(result, 12);
        for (int index = 0; index < BlockCount(data.Length); index++)
            EncryptBlock(data, modulus, fileId, index, result);
        return result;
    }

    private static void EncryptBlock(byte[] data, BigInteger modulus, byte[] fileId, int index, byte[] result)
    {
        BigInteger message = BlockEncoding.Encode(data, fileId, index);
        byte[] encrypted = RabinCipher.Encrypt(message, modulus).ToByteArray(isUnsigned: true, isBigEndian: true);
        int offset = HeaderSize + (index + 1) * CipherBlockSize - encrypted.Length;
        encrypted.CopyTo(result, offset);
    }

    public static byte[] Decrypt(byte[] data, RabinCipher cipher)
    {
        int length = ReadLength(data);
        byte[] fileId = data[12..HeaderSize];
        byte[] result = new byte[length];
        for (int index = 0; index < BlockCount(length); index++)
        {
            var value = new BigInteger(data.AsSpan(HeaderSize + index * CipherBlockSize, CipherBlockSize), true, true);
            DecodeRoot(cipher, value, fileId, index, length).CopyTo(result, index * BlockEncoding.PayloadSize);
        }
        return result;
    }

    private static byte[] DecodeRoot(RabinCipher cipher, BigInteger value, byte[] fileId, int index, int length)
    {
        byte[]? decoded = null;
        foreach (BigInteger root in cipher.GetRoots(value).Distinct())
        {
            if (RabinCipher.Encrypt(root, cipher.Modulus) != value) continue;
            byte[]? candidate = BlockEncoding.Decode(root, fileId, index, length);
            if (candidate is null) continue;
            if (decoded is not null) throw new CryptographicException("Неоднозначное расшифрование блока.");
            decoded = candidate;
        }
        return decoded ?? throw new CryptographicException("Неверный ключ или повреждённый файл.");
    }

    private static int ReadLength(byte[] data)
    {
        if (data.Length < HeaderSize || !data.AsSpan(0, 8).SequenceEqual("RABIN03\0"u8))
            throw new CryptographicException("Это не шифротекст третьей лабораторной.");
        int length = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8));
        if (length < 0 || HeaderSize + (long)BlockCount(length) * CipherBlockSize != data.Length)
            throw new CryptographicException("Некорректная длина шифротекста.");
        return length;
    }

    private static int BlockCount(int length) => length == 0 ? 1 : (length - 1) / BlockEncoding.PayloadSize + 1;
}
