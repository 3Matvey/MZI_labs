namespace MziLab;

// Коэффициенты полинома хранятся от младшей степени к старшей.
internal sealed class Polynomial
{
    private readonly ushort[] _coefficients;
    public int Degree => _coefficients.Length - 1;
    public ushort this[int index] => index < _coefficients.Length ? _coefficients[index] : (ushort)0;
    public static Polynomial Zero { get; } = new();
    public static Polynomial One { get; } = new(1);
    public static Polynomial X { get; } = new(0, 1);

    public Polynomial(params ushort[] coefficients)
    {
        int size = coefficients.Length;
        while (size > 0 && coefficients[size - 1] == 0) size--;
        _coefficients = coefficients[..size];
    }

    public Polynomial Add(Polynomial other)
    {
        var sum = new ushort[Math.Max(Degree, other.Degree) + 1];
        for (int i = 0; i < sum.Length; i++) sum[i] = (ushort)(this[i] ^ other[i]);
        return new Polynomial(sum);
    }

    public Polynomial Multiply(Polynomial other)
    {
        if (Degree < 0 || other.Degree < 0) return Zero;
        var product = new ushort[Degree + other.Degree + 1];
        for (int i = 0; i <= Degree; i++)
            for (int j = 0; j <= other.Degree; j++)
                product[i + j] ^= Gf1024.Multiply(this[i], other[j]);
        return new Polynomial(product);
    }

    public Polynomial SquareMod(Polynomial modulus)
    {
        if (Degree < 0) return Zero;
        var square = new ushort[2 * Degree + 1];
        for (int i = 0; i <= Degree; i++) square[2 * i] = Gf1024.Multiply(this[i], this[i]);
        return new Polynomial(square).Mod(modulus);
    }

    public (Polynomial Quotient, Polynomial Remainder) Divide(Polynomial divisor)
    {
        if (divisor.Degree < 0) throw new DivideByZeroException();
        var remainder = (ushort[])_coefficients.Clone();
        var quotient = new ushort[Math.Max(0, Degree - divisor.Degree + 1)];
        ushort inverse = Gf1024.Inverse(divisor[divisor.Degree]);
        for (int shift = Degree - divisor.Degree; shift >= 0; shift--)
        {
            ushort factor = Gf1024.Multiply(remainder[shift + divisor.Degree], inverse);
            quotient[shift] = factor;
            for (int j = 0; j <= divisor.Degree; j++)
                remainder[shift + j] ^= Gf1024.Multiply(factor, divisor[j]);
        }
        return (new Polynomial(quotient), new Polynomial(remainder));
    }

    public Polynomial Mod(Polynomial modulus) => Divide(modulus).Remainder;

    public Polynomial InverseMod(Polynomial modulus)
    {
        Polynomial oldR = modulus, r = Mod(modulus);
        Polynomial oldT = Zero, t = One;
        while (r.Degree >= 0)
        {
            var (quotient, remainder) = oldR.Divide(r);
            (oldR, r) = (r, remainder);
            (oldT, t) = (t, oldT.Add(quotient.Multiply(t)));
        }
        if (oldR.Degree != 0) throw new InvalidOperationException("Полином не обратим.");
        return oldT.Scale(Gf1024.Inverse(oldR[0])).Mod(modulus);
    }

    public Polynomial Scale(ushort factor)
    {
        var scaled = new ushort[_coefficients.Length];
        for (int i = 0; i < scaled.Length; i++) scaled[i] = Gf1024.Multiply(this[i], factor);
        return new Polynomial(scaled);
    }

    public ushort Evaluate(ushort value)
    {
        ushort result = 0;
        for (int i = Degree; i >= 0; i--)
            result = (ushort)(Gf1024.Multiply(result, value) ^ this[i]);
        return result;
    }

    public static Polynomial Gcd(Polynomial left, Polynomial right)
    {
        while (right.Degree >= 0) (left, right) = (right, left.Mod(right));
        return left;
    }
}
