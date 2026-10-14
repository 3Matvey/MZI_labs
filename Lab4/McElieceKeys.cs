using System.Security.Cryptography;
using System.Text;

namespace MziLab;

internal sealed record PrivateKey(GoppaCode Code, int[] Permutation,
    int[] InformationColumns, BitVector[] InverseScrambler);

internal static class McElieceKeys
{
    private static readonly byte[] PublicMagic = Encoding.ASCII.GetBytes("MCL4PUB1");
    private static readonly byte[] PrivateMagic = Encoding.ASCII.GetBytes("MCL4SEC1");
    private const int K = GoppaCode.MessageBits;
    private const int N = GoppaCode.Length;
    private const int PublicSize = 8 + K * (N / 8);
    private const int PrivateSize = 8 + 2 * (GoppaCode.Errors + 1 + N + K) + K * ((K + 7) / 8);

    public static void Generate(string directory)
    {
        GoppaCode code = GoppaCode.Generate();
        BitVector[] parity = code.ParityRows();
        int[] pivots = Systematize(parity);
        int[] information = Enumerable.Range(0, N).Except(pivots).ToArray();
        BitVector[] generator = BuildGenerator(parity, pivots, information);
        var (scrambler, inverse) = RandomScrambler();
        int[] permutation = RandomPermutation();
        BitVector[] publicRows = HideGenerator(generator, scrambler, permutation);
        SavePublic(Path.Combine(directory, "public-key.bin"), publicRows);
        SavePrivate(Path.Combine(directory, "private-key.bin"),
            new PrivateKey(code, permutation, information, inverse));
    }

    public static BitVector[] ReadPublic(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        CheckFile(data, PublicMagic, PublicSize);
        return Enumerable.Range(0, K)
            .Select(i => BitVector.FromBytes(data.AsSpan(8 + i * (N / 8), N / 8), N))
            .ToArray();
    }

    public static PrivateKey ReadPrivate(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        CheckFile(data, PrivateMagic, PrivateSize);
        using var reader = new BinaryReader(new MemoryStream(data, 8, data.Length - 8));
        var goppa = new Polynomial(ReadShorts(reader, GoppaCode.Errors + 1));
        if (goppa[GoppaCode.Errors] != 1 || Enumerable.Range(0, GoppaCode.Errors).Any(i => goppa[i] >= 1024))
            throw new CryptographicException("Полином закрытого ключа некорректен.");
        int[] permutation = ReadShorts(reader, N).Select(x => (int)x).ToArray();
        int[] information = ReadShorts(reader, K).Select(x => (int)x).ToArray();
        BitVector[] inverse = Enumerable.Range(0, K)
            .Select(_ => BitVector.FromBytes(reader.ReadBytes((K + 7) / 8), K)).ToArray();
        ValidatePositions(permutation, information);
        return new PrivateKey(new GoppaCode(goppa), permutation, information, inverse);
    }

    private static void CheckFile(byte[] data, byte[] magic, int size)
    {
        if (data.Length != size || !data.AsSpan(0, magic.Length).SequenceEqual(magic))
            throw new CryptographicException("Выбран не тот файл ключа или он повреждён.");
    }

    private static ushort[] ReadShorts(BinaryReader reader, int count)
    {
        var values = new ushort[count];
        for (int i = 0; i < count; i++) values[i] = reader.ReadUInt16();
        return values;
    }

    private static void ValidatePositions(int[] permutation, int[] information)
    {
        if (permutation.Any(x => x >= N) || permutation.Distinct().Count() != N
            || information.Any(x => x >= N) || information.Distinct().Count() != K)
            throw new CryptographicException("Закрытый ключ повреждён.");
    }

    private static int[] Systematize(BitVector[] parity)
    {
        var pivots = new int[parity.Length];
        int rank = 0;
        for (int column = 0; column < N && rank < parity.Length; column++)
        {
            int pivot = Array.FindIndex(parity, rank, row => row.Get(column));
            if (pivot < 0) continue;
            (parity[rank], parity[pivot]) = (parity[pivot], parity[rank]);
            for (int row = 0; row < parity.Length; row++)
                if (row != rank && parity[row].Get(column)) parity[row].Xor(parity[rank]);
            pivots[rank++] = column;
        }
        if (rank != parity.Length) throw new CryptographicException("Порождающая матрица неполного ранга.");
        return pivots;
    }

    private static BitVector[] BuildGenerator(BitVector[] parity, int[] pivots, int[] information)
    {
        var rows = new BitVector[K];
        for (int i = 0; i < K; i++)
        {
            var row = new BitVector(N);
            row.Set(information[i]);
            for (int j = 0; j < pivots.Length; j++)
                if (parity[j].Get(information[i])) row.Set(pivots[j]);
            rows[i] = row;
        }
        return rows;
    }

    private static (BitVector[] Matrix, BitVector[] Inverse) RandomScrambler()
    {
        while (true)
        {
            BitVector[] matrix = Enumerable.Range(0, K)
                .Select(_ => BitVector.FromBytes(RandomNumberGenerator.GetBytes((K + 7) / 8), K))
                .ToArray();
            BitVector[]? inverse = Invert(matrix);
            if (inverse is not null) return (matrix, inverse);
        }
    }

    private static BitVector[]? Invert(BitVector[] matrix)
    {
        BitVector[] work = matrix.Select(row => BitVector.FromBytes(row.ToBytes(), K)).ToArray();
        BitVector[] inverse = Enumerable.Range(0, K).Select(i => Unit(i, K)).ToArray();
        for (int column = 0; column < K; column++)
        {
            int pivot = Array.FindIndex(work, column, row => row.Get(column));
            if (pivot < 0) return null;
            (work[column], work[pivot]) = (work[pivot], work[column]);
            (inverse[column], inverse[pivot]) = (inverse[pivot], inverse[column]);
            for (int row = 0; row < K; row++)
                if (row != column && work[row].Get(column))
                { work[row].Xor(work[column]); inverse[row].Xor(inverse[column]); }
        }
        return inverse;
    }

    private static BitVector Unit(int index, int length)
    {
        var vector = new BitVector(length);
        vector.Set(index);
        return vector;
    }

    private static int[] RandomPermutation()
    {
        int[] permutation = Enumerable.Range(0, N).ToArray();
        for (int i = N - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        }
        return permutation;
    }

    private static BitVector[] HideGenerator(BitVector[] generator,
        BitVector[] scrambler, int[] permutation)
    {
        var rows = new BitVector[K];
        for (int i = 0; i < K; i++)
        {
            var mixed = new BitVector(N);
            foreach (int j in scrambler[i].SetBits()) mixed.Xor(generator[j]);
            rows[i] = new BitVector(N);
            foreach (int oldIndex in mixed.SetBits()) rows[i].Set(permutation[oldIndex]);
        }
        return rows;
    }

    private static void SavePublic(string path, BitVector[] rows)
    {
        using var writer = NewWriter(path);
        writer.Write(PublicMagic);
        foreach (BitVector row in rows) writer.Write(row.ToBytes());
    }

    private static void SavePrivate(string path, PrivateKey key)
    {
        using var writer = NewWriter(path);
        writer.Write(PrivateMagic);
        for (int i = 0; i <= GoppaCode.Errors; i++) writer.Write(key.Code.Goppa[i]);
        foreach (int position in key.Permutation) writer.Write((ushort)position);
        foreach (int position in key.InformationColumns) writer.Write((ushort)position);
        foreach (BitVector row in key.InverseScrambler) writer.Write(row.ToBytes());
    }

    private static BinaryWriter NewWriter(string path) =>
        new(new FileStream(path, FileMode.CreateNew, FileAccess.Write), Encoding.UTF8);
}
