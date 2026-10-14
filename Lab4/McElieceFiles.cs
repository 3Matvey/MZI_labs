using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace MziLab;

internal static class McElieceFiles
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("MCL4ENC1");
    private const int PlainBlockSize = 80;
    private const int CipherBlockSize = GoppaCode.Length / 8;
    private const int PayloadHeaderSize = 8 + 32;

    public static void Transform(string input, string key, string output, bool decrypt)
    {
        if (decrypt) Decrypt(input, key, output);
        else Encrypt(input, key, output);
    }

    private static void Encrypt(string input, string key, string output)
    {
        BitVector[] publicRows = McElieceKeys.ReadPublic(key);
        byte[] payload = Pack(File.ReadAllBytes(input));
        int count = 1 + (payload.Length - 1) / PlainBlockSize;
        byte[] result = new byte[checked(Magic.Length + count * CipherBlockSize)];
        Magic.CopyTo(result, 0);
        for (int i = 0; i < count; i++)
        {
            int length = Math.Min(PlainBlockSize, payload.Length - i * PlainBlockSize);
            BitVector message = BitVector.FromBytes(payload.AsSpan(i * PlainBlockSize, length), GoppaCode.MessageBits);
            EncryptBlock(message, publicRows).ToBytes().CopyTo(result, Magic.Length + i * CipherBlockSize);
        }
        SaveNew(output, result);
    }

    private static void Decrypt(string input, string key, string output)
    {
        byte[] cipher = File.ReadAllBytes(input);
        int count = CheckCiphertext(cipher);
        PrivateKey privateKey = McElieceKeys.ReadPrivate(key);
        byte[] payload = new byte[count * PlainBlockSize];
        for (int i = 0; i < count; i++)
        {
            BitVector block = BitVector.FromBytes(
                cipher.AsSpan(Magic.Length + i * CipherBlockSize, CipherBlockSize), GoppaCode.Length);
            BitVector message = DecryptBlock(block, privateKey);
            if (message.SetBits().Any(bit => bit >= PlainBlockSize * 8))
                throw new CryptographicException("Неверный ключ или повреждённый файл.");
            message.ToBytes().AsSpan(0, PlainBlockSize).CopyTo(payload.AsSpan(i * PlainBlockSize));
        }
        SaveNew(output, Unpack(payload, count));
    }

    private static BitVector EncryptBlock(BitVector message, BitVector[] publicRows)
    {
        var result = new BitVector(GoppaCode.Length);
        foreach (int bit in message.SetBits()) result.Xor(publicRows[bit]);
        var errors = new BitVector(GoppaCode.Length);
        int count = 0;
        while (count < GoppaCode.Errors)
        {
            int index = RandomNumberGenerator.GetInt32(GoppaCode.Length);
            if (errors.Get(index)) continue;
            errors.Set(index);
            count++;
        }
        result.Xor(errors);
        return result;
    }

    private static BitVector DecryptBlock(BitVector cipher, PrivateKey key)
    {
        var originalOrder = new BitVector(GoppaCode.Length);
        for (int i = 0; i < GoppaCode.Length; i++)
            if (cipher.Get(key.Permutation[i])) originalOrder.Set(i);
        BitVector corrected = key.Code.Decode(originalOrder);
        var scrambled = new BitVector(GoppaCode.MessageBits);
        for (int i = 0; i < GoppaCode.MessageBits; i++)
            if (corrected.Get(key.InformationColumns[i])) scrambled.Set(i);
        var message = new BitVector(GoppaCode.MessageBits);
        foreach (int bit in scrambled.SetBits()) message.Xor(key.InverseScrambler[bit]);
        return message;
    }

    private static byte[] Pack(byte[] source)
    {
        byte[] payload = new byte[checked(source.Length + PayloadHeaderSize)];
        BinaryPrimitives.WriteInt64LittleEndian(payload, source.LongLength);
        SHA256.HashData(source).CopyTo(payload, 8);
        source.CopyTo(payload, PayloadHeaderSize);
        return payload;
    }

    private static byte[] Unpack(byte[] payload, int blockCount)
    {
        long length = BinaryPrimitives.ReadInt64LittleEndian(payload);
        if (length < 0 || length > payload.Length - PayloadHeaderSize
            || (length + PayloadHeaderSize + PlainBlockSize - 1) / PlainBlockSize != blockCount)
            throw new CryptographicException("Неверный ключ или повреждённый файл.");
        byte[] source = payload.AsSpan(PayloadHeaderSize, (int)length).ToArray();
        if (!SHA256.HashData(source).AsSpan().SequenceEqual(payload.AsSpan(8, 32))
            || payload.AsSpan(PayloadHeaderSize + (int)length).IndexOfAnyExcept((byte)0) >= 0)
            throw new CryptographicException("Неверный ключ или повреждённый файл.");
        return source;
    }

    private static int CheckCiphertext(byte[] data)
    {
        int length = data.Length - Magic.Length;
        if (length < CipherBlockSize || length % CipherBlockSize != 0
            || !data.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            throw new CryptographicException("Это не шифрофайл лабораторной № 4.");
        return length / CipherBlockSize;
    }

    private static void SaveNew(string path, byte[] data)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        file.Write(data);
    }
}
