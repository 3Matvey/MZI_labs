using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace MziLab;

// 255 байт всегда меньше 2048-битного n. Полезные данные окружены случайными байтами и хешем.
internal static class BlockEncoding
{
    public const int EncodedSize = 255;
    public const int PayloadOffset = 60;
    public const int PayloadSize = 163;
    private const int HashOffset = EncodedSize - 32;

    public static BigInteger Encode(byte[] data, byte[] fileId, int index)
    {
        byte[] block = RandomNumberGenerator.GetBytes(EncodedSize);
        "RB03"u8.CopyTo(block);
        fileId.CopyTo(block, 4);
        BinaryPrimitives.WriteInt32LittleEndian(block.AsSpan(20), index);
        BinaryPrimitives.WriteInt32LittleEndian(block.AsSpan(24), data.Length);
        int offset = index * PayloadSize;
        data.AsSpan(offset, Math.Min(PayloadSize, data.Length - offset)).CopyTo(block.AsSpan(PayloadOffset));
        SHA256.HashData(block.AsSpan(0, HashOffset)).CopyTo(block, HashOffset);
        return new BigInteger(block, isUnsigned: true, isBigEndian: true);
    }

    public static byte[]? Decode(BigInteger root, byte[] fileId, int index, int length)
    {
        byte[] block = root.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (block.Length != EncodedSize || !block.AsSpan(0, 4).SequenceEqual("RB03"u8)) return null;
        if (!block.AsSpan(4, 16).SequenceEqual(fileId)) return null;
        if (BinaryPrimitives.ReadInt32LittleEndian(block.AsSpan(20)) != index) return null;
        if (BinaryPrimitives.ReadInt32LittleEndian(block.AsSpan(24)) != length) return null;
        byte[] hash = SHA256.HashData(block.AsSpan(0, HashOffset));
        if (!CryptographicOperations.FixedTimeEquals(hash, block.AsSpan(HashOffset))) return null;
        int count = Math.Min(PayloadSize, length - index * PayloadSize);
        return block.AsSpan(PayloadOffset, count).ToArray();
    }
}
