using System.Buffers.Binary;

namespace MziLab;

public abstract class BlockCipher
{
    public abstract int BlockSize { get; }
    public abstract byte[] EncryptBlock(byte[] block);
    public abstract byte[] DecryptBlock(byte[] block);

    protected static uint Read(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)));

    protected static byte[] Words(params uint[] words)
    {
        byte[] result = new byte[words.Length * sizeof(uint)];
        for (int index = 0; index < words.Length; index++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(index * sizeof(uint)), words[index]);
        }
        return result;
    }

    protected static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message);
        }
    }

    public byte[] EcbEncrypt(byte[] data) => TransformBlocks(AddPadding(data), EncryptBlock);

    public byte[] EcbDecrypt(byte[] data)
    {
        Require(data.Length > 0 && data.Length % BlockSize == 0,
            "Некорректная длина шифротекста ECB");
        return RemovePadding(TransformBlocks(data, DecryptBlock));
    }

    private byte[] AddPadding(byte[] data)
    {
        // PKCS#7 — файловый формат приложения, не часть базового шифра.
        int paddingSize = BlockSize - data.Length % BlockSize;
        byte[] result = new byte[data.Length + paddingSize];
        data.CopyTo(result, 0);
        result.AsSpan(data.Length).Fill((byte)paddingSize);
        return result;
    }

    private byte[] RemovePadding(byte[] data)
    {
        int paddingSize = data[^1];
        Require(paddingSize >= 1 && paddingSize <= BlockSize, "Неверный ключ или дополнение PKCS#7");
        for (int index = data.Length - paddingSize; index < data.Length; index++)
        {
            Require(data[index] == paddingSize, "Повреждено дополнение PKCS#7");
        }
        return data[..^paddingSize];
    }

    private byte[] TransformBlocks(byte[] data, Func<byte[], byte[]> transform)
    {
        byte[] result = new byte[data.Length];
        for (int offset = 0; offset < data.Length; offset += BlockSize)
        {
            byte[] block = data[offset..(offset + BlockSize)];
            transform(block).CopyTo(result, offset);
        }
        return result;
    }

    public byte[] Cfb(byte[] data, byte[] iv, bool decrypt = false)
    {
        Require(iv.Length == BlockSize, "Неверная длина IV");
        byte[] feedback = iv;
        byte[] result = new byte[data.Length];
        for (int offset = 0; offset < data.Length; offset += BlockSize)
        {
            XorBlock(data, offset, EncryptBlock(feedback), result);
            feedback = (decrypt ? data : result)[offset..Math.Min(offset + BlockSize, data.Length)];
        }
        return result;
    }

    protected static void XorBlock(byte[] data, int offset, byte[] gamma, byte[] result)
    {
        int count = Math.Min(gamma.Length, data.Length - offset);
        for (int index = 0; index < count; index++)
        {
            result[offset + index] = (byte)(data[offset + index] ^ gamma[index]);
        }
    }
}
