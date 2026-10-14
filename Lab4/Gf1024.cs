namespace MziLab;

// GF(2^10) = GF(2)[x] / (x^10 + x^3 + 1).
internal static class Gf1024
{
    private const int Reduction = 0x409;
    public const int Size = 1024;

    public static ushort Multiply(ushort left, ushort right)
    {
        int result = 0;
        int a = left;
        int b = right;
        while (b != 0)
        {
            if ((b & 1) != 0) result ^= a;
            a <<= 1;
            if ((a & Size) != 0) a ^= Reduction;
            b >>= 1;
        }
        return (ushort)result;
    }

    public static ushort Inverse(ushort value)
    {
        if (value == 0) throw new DivideByZeroException();
        ushort result = 1;
        for (int power = Size - 2; power > 0; power >>= 1)
        {
            if ((power & 1) != 0) result = Multiply(result, value);
            value = Multiply(value, value);
        }
        return result;
    }
}
