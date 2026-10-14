using System.Security.Cryptography;

namespace MziLab;

internal sealed class GoppaCode
{
    public const int Length = 1024;
    public const int Errors = 38;
    public const int CheckBits = 10 * Errors;
    public const int MessageBits = Length - CheckBits;

    public Polynomial Goppa { get; }
    private readonly ushort[][] _columns;

    public GoppaCode(Polynomial goppa)
    {
        if (goppa.Degree != Errors) throw new ArgumentException("Неверная степень полинома.");
        Goppa = goppa;
        _columns = CreateColumns(goppa);
    }

    public static GoppaCode Generate()
    {
        while (true)
        {
            var coefficients = new ushort[Errors + 1];
            byte[] random = RandomNumberGenerator.GetBytes(Errors * 2);
            for (int i = 0; i < Errors; i++)
                coefficients[i] = (ushort)((random[2 * i] | random[2 * i + 1] << 8) & 1023);
            coefficients[Errors] = 1;
            var candidate = new Polynomial(coefficients);
            if (IsIrreducible(candidate)) return new GoppaCode(candidate);
        }
    }

    public BitVector[] ParityRows()
    {
        var rows = Enumerable.Range(0, CheckBits).Select(_ => new BitVector(Length)).ToArray();
        for (int column = 0; column < Length; column++)
            for (int degree = 0; degree < Errors; degree++)
                for (int bit = 0; bit < 10; bit++)
                    if (((_columns[column][degree] >> bit) & 1) != 0)
                        rows[degree * 10 + bit].Set(column);
        return rows;
    }

    public BitVector Decode(BitVector received)
    {
        Polynomial syndrome = Syndrome(received);
        if (syndrome.Degree < 0) return received;
        Polynomial inverse = syndrome.InverseMod(Goppa);
        Polynomial root = SquareRoot(Polynomial.X.Add(inverse).Mod(Goppa));
        var (a, b) = Approximate(root);
        Polynomial locator = a.Multiply(a).Add(Polynomial.X.Multiply(b.Multiply(b)));
        BitVector errors = FindErrors(locator);
        if (Syndrome(errors).Add(syndrome).Degree >= 0)
            throw new CryptographicException("Неверный ключ или повреждённый файл.");
        received.Xor(errors);
        return received;
    }

    private Polynomial Syndrome(BitVector word)
    {
        var coefficients = new ushort[Errors];
        foreach (int index in word.SetBits())
            for (int j = 0; j < Errors; j++) coefficients[j] ^= _columns[index][j];
        return new Polynomial(coefficients);
    }

    private Polynomial SquareRoot(Polynomial value)
    {
        for (int i = 0; i < 10 * Errors - 1; i++) value = value.SquareMod(Goppa);
        return value;
    }

    private (Polynomial A, Polynomial B) Approximate(Polynomial value)
    {
        Polynomial oldR = Goppa, r = value;
        Polynomial oldB = Polynomial.Zero, b = Polynomial.One;
        while (r.Degree > Errors / 2)
        {
            var (quotient, remainder) = oldR.Divide(r);
            (oldR, r) = (r, remainder);
            (oldB, b) = (b, oldB.Add(quotient.Multiply(b)));
        }
        if (r.Degree > Errors / 2 || b.Degree > (Errors - 1) / 2)
            throw new CryptographicException("Неверный ключ или повреждённый файл.");
        return (r, b);
    }

    private static BitVector FindErrors(Polynomial locator)
    {
        var errors = new BitVector(Length);
        int count = 0;
        for (ushort index = 0; index < Length; index++)
            if (locator.Evaluate(index) == 0) { errors.Set(index); count++; }
        if (count > Errors) throw new CryptographicException("Слишком много ошибок в блоке.");
        return errors;
    }

    private static ushort[][] CreateColumns(Polynomial goppa)
    {
        var columns = new ushort[Length][];
        for (ushort alpha = 0; alpha < Length; alpha++)
        {
            var quotient = new ushort[Errors];
            quotient[^1] = 1;
            for (int j = Errors - 1; j > 0; j--)
                quotient[j - 1] = (ushort)(goppa[j] ^ Gf1024.Multiply(alpha, quotient[j]));
            ushort remainder = (ushort)(goppa[0] ^ Gf1024.Multiply(alpha, quotient[0]));
            if (remainder == 0) throw new CryptographicException("Полином закрытого ключа некорректен.");
            ushort inverse = Gf1024.Inverse(remainder);
            for (int j = 0; j < Errors; j++) quotient[j] = Gf1024.Multiply(quotient[j], inverse);
            columns[alpha] = quotient;
        }
        return columns;
    }

    private static bool IsIrreducible(Polynomial goppa)
    {
        Polynomial power = Polynomial.X;
        for (int i = 1; i <= Errors; i++)
        {
            for (int bit = 0; bit < 10; bit++) power = power.SquareMod(goppa);
            if ((i == 2 || i == 19) && Polynomial.Gcd(power.Add(Polynomial.X), goppa).Degree > 0)
                return false;
        }
        return power.Add(Polynomial.X).Degree < 0;
    }
}
