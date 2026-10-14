using System.Numerics;

namespace MziLab;

internal sealed class BitVector
{
    public int Length { get; }
    public ulong[] Words { get; }

    public BitVector(int length)
    {
        Length = length;
        Words = new ulong[(length + 63) / 64];
    }

    public bool Get(int index) => (Words[index / 64] & (1UL << (index % 64))) != 0;
    public void Set(int index) => Words[index / 64] |= 1UL << (index % 64);
    public void Toggle(int index) => Words[index / 64] ^= 1UL << (index % 64);

    public void Xor(BitVector other)
    {
        for (int i = 0; i < Words.Length; i++) Words[i] ^= other.Words[i];
    }

    public IEnumerable<int> SetBits()
    {
        for (int i = 0; i < Words.Length; i++)
        {
            ulong word = Words[i];
            while (word != 0)
            {
                int index = i * 64 + BitOperations.TrailingZeroCount(word);
                if (index < Length) yield return index;
                word &= word - 1;
            }
        }
    }

    public byte[] ToBytes()
    {
        byte[] bytes = new byte[(Length + 7) / 8];
        Buffer.BlockCopy(Words, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static BitVector FromBytes(ReadOnlySpan<byte> bytes, int length)
    {
        if (bytes.Length > (length + 7) / 8) throw new ArgumentException("Слишком много данных.");
        var result = new BitVector(length);
        Buffer.BlockCopy(bytes.ToArray(), 0, result.Words, 0, bytes.Length);
        if (length % 64 != 0) result.Words[^1] &= (1UL << (length % 64)) - 1;
        return result;
    }
}
