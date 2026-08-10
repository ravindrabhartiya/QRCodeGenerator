using System;

namespace QrGen;

/// <summary>
/// Reed–Solomon error-correction codeword generation over GF(256), as used by QR Codes.
/// </summary>
public static class ReedSolomon
{
    /// <summary>
    /// Builds the monic Reed–Solomon generator polynomial of the given degree.
    /// Coefficients are returned highest-degree first (index 0 is the leading 1).
    /// </summary>
    /// <param name="degree">Number of error-correction codewords (polynomial degree).</param>
    public static byte[] GeneratorPolynomial(int degree)
    {
        if (degree < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(degree), "Degree must be at least 1.");
        }

        byte[] poly = { 1 };
        for (int i = 0; i < degree; i++)
        {
            var next = new byte[poly.Length + 1];
            for (int j = 0; j < poly.Length; j++)
            {
                next[j] ^= poly[j];
                next[j + 1] ^= GaloisField.Multiply(poly[j], GaloisField.Exp(i));
            }

            poly = next;
        }

        return poly;
    }

    /// <summary>
    /// Computes the <paramref name="ecCount"/> Reed–Solomon error-correction codewords
    /// for the supplied data codewords.
    /// </summary>
    public static byte[] ComputeEc(byte[] data, int ecCount)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (ecCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ecCount), "Error-correction count must be at least 1.");
        }

        byte[] gen = GeneratorPolynomial(ecCount);
        var res = new byte[data.Length + ecCount];
        Array.Copy(data, res, data.Length);

        for (int i = 0; i < data.Length; i++)
        {
            byte coef = res[i];
            if (coef != 0)
            {
                for (int j = 0; j < gen.Length; j++)
                {
                    res[i + j] ^= GaloisField.Multiply(gen[j], coef);
                }
            }
        }

        var ec = new byte[ecCount];
        Array.Copy(res, data.Length, ec, 0, ecCount);
        return ec;
    }
}
