using System.Numerics;
using System.Security.Cryptography;

namespace MziLab;

internal sealed class RabinCipher(BigInteger p, BigInteger q)
{
    public BigInteger Modulus { get; } = p * q;
    private readonly BigInteger _qPart = q * BigInteger.ModPow(q, p - 2, p);
    private readonly BigInteger _pPart = p * BigInteger.ModPow(p, q - 2, q);

    public static BigInteger Encrypt(BigInteger message, BigInteger modulus) => message * message % modulus;

    public BigInteger[] GetRoots(BigInteger ciphertext)
    {
        if (ciphertext < 0 || ciphertext >= Modulus)
            throw new CryptographicException("Блок шифротекста вне допустимого диапазона.");
        BigInteger rootP = BigInteger.ModPow(ciphertext, (p + 1) / 4, p);
        BigInteger rootQ = BigInteger.ModPow(ciphertext, (q + 1) / 4, q);
        BigInteger first = Normalize(rootP * _qPart + rootQ * _pPart);
        BigInteger second = Normalize(rootP * _qPart - rootQ * _pPart);
        return [first, Normalize(-first), second, Normalize(-second)];
    }

    private BigInteger Normalize(BigInteger value) => (value % Modulus + Modulus) % Modulus;
}
