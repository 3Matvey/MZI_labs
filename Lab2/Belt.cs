using System.Numerics;

namespace MziLab;

// BelT, СТБ 34.101.31-2011. Ключ 256 бит; слова little-endian.
public sealed class Belt : BlockCipher
{
    public override int BlockSize => 16;
    private readonly uint[] _keys = new uint[56];
    private static readonly byte[] SubstitutionBox = Convert.FromHexString(
        "B194BAC80A08F53B366D008E584A5DE48504FA9D1BB6C7AC252E72C202FDCE0D" +
        "5BE3D61217B96181FE6786AD716B890B5CB0C0FF33C356B835C405AED8E07F99" +
        "E12BDC1AE28257EC703FCCF095EE8DF1C1AB76389FE678CAF7C6F860D5BB9C4F" +
        "F33C657B637C306ADD4EA7799EB23D313E98B56E27D3BCCF591E181F4C5AB793" +
        "E9DEE72C8F0C0FA62DDB49F46F73964706075316ED247A3739CBA38303A98BF6" +
        "92BD9B1CE5D141015445FBC95E4D0EF2682080AA227D642F2687F93490405511" +
        "BE32971343FC9A48A02A885F194B09A17ECDA4D01544AF8CA58450BF66D2E88A" +
        "A2D7465242A8DFB36974C551EB232921D4EFD9B43A622875911410EA776CDA1D");

    public Belt(byte[] key)
    {
        Require(key.Length == 32, "Ключ BelT должен содержать 32 байта");
        for (int index = 0; index < _keys.Length; index++)
        {
            _keys[index] = Read(key, (index % 8) * sizeof(uint));
        }
    }

    private static uint SubstituteAndRotate(uint input, int rotation)
    {
        uint result = 0;
        for (int index = 0; index < sizeof(uint); index++)
        {
            int shift = index * 8;
            result |= (uint)SubstitutionBox[(input >> shift) & 255] << shift;
        }
        return BitOperations.RotateLeft(result, rotation);
    }

    public override byte[] EncryptBlock(byte[] block)
    {
        Require(block.Length == BlockSize, "Блок BelT должен содержать 16 байт");
        var state = (A: Read(block, 0), B: Read(block, 4), C: Read(block, 8), D: Read(block, 12));
        for (int round = 1; round <= 8; round++)
        {
            state = EncryptRound(state, round);
        }
        return Words(state.B, state.D, state.A, state.C);
    }

    public override byte[] DecryptBlock(byte[] block)
    {
        Require(block.Length == BlockSize, "Блок BelT должен содержать 16 байт");
        var state = (A: Read(block, 8), B: Read(block, 0), C: Read(block, 12), D: Read(block, 4));
        for (int round = 8; round >= 1; round--)
        {
            state = DecryptRound(state, round);
        }
        return Words(state.A, state.B, state.C, state.D);
    }

    // a, b, c, d соответствуют обозначениям слов блока в стандарте.
    private (uint A, uint B, uint C, uint D) EncryptRound(
        (uint A, uint B, uint C, uint D) state, int round)
    {
        var (a, b, c, d) = state;
        int keyOffset = 7 * (round - 1);
        b ^= ApplyKey(a, keyOffset, 5);
        c ^= ApplyKey(d, keyOffset + 1, 21);
        a = unchecked(a - ApplyKey(b, keyOffset + 2, 13));
        (b, c) = MixMiddle(b, c, keyOffset, round);
        d = unchecked(d + ApplyKey(c, keyOffset + 4, 13));
        b ^= ApplyKey(a, keyOffset + 5, 21);
        c ^= ApplyKey(d, keyOffset + 6, 5);
        return (b, d, a, c);
    }

    private (uint A, uint B, uint C, uint D) DecryptRound(
        (uint A, uint B, uint C, uint D) state, int round)
    {
        var (a, b, c, d) = (state.C, state.A, state.D, state.B);
        int keyOffset = 7 * (round - 1);
        c ^= ApplyKey(d, keyOffset + 6, 5);
        b ^= ApplyKey(a, keyOffset + 5, 21);
        d = unchecked(d - ApplyKey(c, keyOffset + 4, 13));
        (b, c) = MixMiddle(b, c, keyOffset, round, decrypt: true);
        a = unchecked(a + ApplyKey(b, keyOffset + 2, 13));
        c ^= ApplyKey(d, keyOffset + 1, 21);
        b ^= ApplyKey(a, keyOffset, 5);
        return (a, b, c, d);
    }

    private uint ApplyKey(uint value, int keyIndex, int rotation) =>
        SubstituteAndRotate(unchecked(value + _keys[keyIndex]), rotation);

    private (uint B, uint C) MixMiddle(uint b, uint c, int keyOffset, int round, bool decrypt = false)
    {
        uint mixed = ApplyKey(unchecked(b + c), keyOffset + 3, 21) ^ (uint)round;
        return decrypt
            ? (unchecked(b - mixed), unchecked(c + mixed))
            : (unchecked(b + mixed), unchecked(c - mixed));
    }
}
