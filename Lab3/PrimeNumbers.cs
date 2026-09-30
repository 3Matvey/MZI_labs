using System.Numerics;
using System.Security.Cryptography;

namespace MziLab;

internal static class PrimeNumbers
{
    private static readonly int[] SmallPrimes = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47];

    public static BigInteger Generate()
    {
        while (true)
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(128);
            bytes[0] |= 0xC0; // Два старших бита гарантируют 2048 бит у произведения p*q.
            bytes[^1] |= 3; // p ≡ 3 (mod 4), чтобы извлекать корень возведением в степень.
            var candidate = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
            if (IsPrime(candidate)) return candidate;
        }
    }

    public static bool IsPrime(BigInteger value)
    {
        if (value < 2) return false;
        foreach (int prime in SmallPrimes)
        {
            if (value == prime) return true;
            if (value % prime == 0) return false;
        }
        return MillerRabin(value);
    }

    private static bool MillerRabin(BigInteger value)
    {
        BigInteger oddPart = value - 1;
        int powersOfTwo = 0;
        while (oddPart.IsEven) { oddPart >>= 1; powersOfTwo++; }
        for (int round = 0; round < 64; round++)
        {
            BigInteger basis = RandomBelow(value - 3) + 2;
            if (!PassesRound(value, basis, oddPart, powersOfTwo)) return false;
        }
        return true;
    }

    private static bool PassesRound(BigInteger value, BigInteger basis, BigInteger oddPart, int powersOfTwo)
    {
        BigInteger power = BigInteger.ModPow(basis, oddPart, value);
        if (power == 1 || power == value - 1) return true;
        for (int index = 1; index < powersOfTwo; index++)
        {
            power = power * power % value;
            if (power == value - 1) return true;
        }
        return false;
    }

    private static BigInteger RandomBelow(BigInteger limit)
    {
        byte[] bytes = new byte[limit.GetByteCount(isUnsigned: true)];
        BigInteger value;
        do
        {
            RandomNumberGenerator.Fill(bytes);
            value = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
        } while (value >= limit);
        return value;
    }
}
