using System.Numerics;
using System.Security.Cryptography;

namespace MziLab;

// ГОСТ 28147-89, таблица замен CryptoPro-A, little-endian.
public sealed class Gost28147 : BlockCipher
{
    public override int BlockSize => 8;
    private readonly uint[] _keys = new uint[8];
    private static readonly byte[,] SubstitutionBox =
    {
        { 9, 6, 3, 2, 8, 11, 1, 7, 10, 4, 14, 15, 12, 0, 13, 5 },
        { 3, 7, 14, 9, 8, 10, 15, 0, 5, 2, 6, 12, 11, 4, 13, 1 },
        { 14, 4, 6, 2, 11, 3, 13, 8, 12, 15, 5, 10, 0, 7, 1, 9 },
        { 14, 7, 10, 12, 13, 1, 3, 9, 0, 2, 11, 4, 15, 8, 5, 6 },
        { 11, 5, 1, 9, 8, 13, 15, 0, 14, 4, 2, 3, 12, 7, 10, 6 },
        { 3, 10, 13, 12, 1, 2, 0, 11, 7, 5, 9, 4, 8, 15, 14, 6 },
        { 1, 13, 2, 9, 7, 10, 6, 0, 8, 12, 4, 5, 15, 3, 11, 14 },
        { 11, 10, 15, 5, 0, 12, 14, 8, 6, 2, 3, 9, 1, 7, 13, 4 }
    };

    public Gost28147(byte[] key)
    {
        Require(key.Length == 32, "Ключ ГОСТ должен содержать 32 байта");
        for (int index = 0; index < _keys.Length; index++)
        {
            _keys[index] = Read(key, index * sizeof(uint));
        }
    }

    private static uint Round(uint value, uint key)
    {
        uint sum = unchecked(value + key);
        uint substituted = 0;
        for (int index = 0; index < 8; index++)
        {
            int shift = index * 4;
            substituted |= (uint)SubstitutionBox[index, (sum >> shift) & 15] << shift;
        }
        return BitOperations.RotateLeft(substituted, 11);
    }

    public override byte[] EncryptBlock(byte[] block) => Cycle(block, decrypt: false);
    public override byte[] DecryptBlock(byte[] block) => Cycle(block, decrypt: true);

    private byte[] Cycle(byte[] block, bool decrypt, bool mac = false)
    {
        Require(block.Length == BlockSize, "Блок ГОСТ должен содержать 8 байт");
        uint left = Read(block, 0);
        uint right = Read(block, 4);
        for (int round = 0; round < (mac ? 16 : 32); round++)
        {
            int keyIndex = mac ? round % 8 : GetKeyIndex(round, decrypt);
            (left, right) = (right ^ Round(left, _keys[keyIndex]), left);
        }
        // Для IMIT конечная перестановка половин не выполняется.
        return mac ? Words(left, right) : Words(right, left);
    }

    private static int GetKeyIndex(int round, bool decrypt)
    {
        if (decrypt)
        {
            return round < 8 ? round : 7 - round % 8;
        }
        return round < 24 ? round % 8 : 7 - round % 8;
    }

    public byte[] Cnt(byte[] data, byte[] iv)
    {
        Require(iv.Length == BlockSize, "IV ГОСТ должен содержать 8 байт");
        byte[] initialState = EncryptBlock(iv);
        var counter = (Low: Read(initialState, 0), High: Read(initialState, 4));
        byte[] result = new byte[data.Length];
        for (int offset = 0; offset < data.Length; offset += BlockSize)
        {
            counter = AdvanceCounter(counter);
            XorBlock(data, offset, EncryptBlock(Words(counter.Low, counter.High)), result);
        }
        return result;
    }

    private static (uint Low, uint High) AdvanceCounter((uint Low, uint High) counter)
    {
        uint low = unchecked(counter.Low + 0x01010101u);
        ulong sum = (ulong)counter.High + 0x01010104u;
        // Старшая половина складывается с переносом по модулю 2^32 - 1.
        uint high = (uint)((sum & uint.MaxValue) + (sum >> 32));
        return (low, high);
    }

    public byte[] Mac(byte[] data, int tagSize = 4)
    {
        Require(data.Length > 0, "Для IMIT нужен непустой файл");
        Require(tagSize is >= 1 and <= 4, "Длина тега: 1–4 байта");
        byte[] state = new byte[BlockSize];
        int blockCount = Math.Max(2, (data.Length - 1) / BlockSize + 1);
        for (int block = 0; block < blockCount; block++)
        {
            MixMacBlock(state, data, block * BlockSize);
            state = Cycle(state, decrypt: false, mac: true);
        }
        return state[..tagSize];
    }

    private static void MixMacBlock(byte[] state, byte[] data, int offset)
    {
        for (int index = 0; index < state.Length && offset + index < data.Length; index++)
        {
            state[index] ^= data[offset + index];
        }
    }

    public bool VerifyMac(byte[] data, byte[] tag) =>
        CryptographicOperations.FixedTimeEquals(Mac(data, tag.Length), tag);
}
