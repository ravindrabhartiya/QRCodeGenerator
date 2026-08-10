using System;

namespace QrGen;

/// <summary>
/// Arithmetic over the Galois field GF(256) used by QR Reed–Solomon error correction.
/// Uses the primitive polynomial 0x11D (x⁸ + x⁴ + x³ + x² + 1) with generator 2.
/// </summary>
public static class GaloisField
{
    private const int Primitive = 0x11D;
    private static readonly byte[] ExpTable = new byte[512];
    private static readonly byte[] LogTable = new byte[256];

    static GaloisField()
    {
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            ExpTable[i] = (byte)x;
            LogTable[x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0)
            {
                x ^= Primitive;
            }
        }

        for (int i = 255; i < 512; i++)
        {
            ExpTable[i] = ExpTable[i - 255];
        }
    }

    /// <summary>Returns α raised to the given exponent (α = 2), reduced modulo 255.</summary>
    public static byte Exp(int exponent)
    {
        int e = exponent % 255;
        if (e < 0)
        {
            e += 255;
        }

        return ExpTable[e];
    }

    /// <summary>Returns the discrete logarithm (base α) of a non-zero field element.</summary>
    public static byte Log(int value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Log(0) is undefined in GF(256).");
        }

        return LogTable[value & 0xFF];
    }

    /// <summary>Multiplies two field elements.</summary>
    public static byte Multiply(int a, int b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        return ExpTable[LogTable[a & 0xFF] + LogTable[b & 0xFF]];
    }
}
