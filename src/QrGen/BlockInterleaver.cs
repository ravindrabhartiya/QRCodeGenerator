using System;
using System.Collections.Generic;

namespace QrGen;

/// <summary>
/// Splits the data codewords into Reed–Solomon blocks, computes each block's
/// error-correction codewords, and interleaves them into the final codeword sequence
/// as required by the QR specification (§7.6).
/// </summary>
public static class BlockInterleaver
{
    /// <summary>
    /// Produces the final interleaved codeword sequence for Version 4 at the given EC level.
    /// The result contains all data codewords (interleaved) followed by all error-correction
    /// codewords (interleaved), totalling <see cref="Version4.TotalCodewords"/> bytes.
    /// </summary>
    /// <param name="dataCodewords">The padded data codewords for this version and EC level.</param>
    /// <param name="ec">The error-correction level determining the block layout.</param>
    public static byte[] Interleave(byte[] dataCodewords, EcLevel ec)
    {
        if (dataCodewords is null)
        {
            throw new ArgumentNullException(nameof(dataCodewords));
        }

        var (blocks, ecPerBlock, dataPerBlock) = Version4.BlockLayout(ec);
        int expected = blocks * dataPerBlock;
        if (dataCodewords.Length != expected)
        {
            throw new ArgumentException(
                $"Expected {expected} data codewords for EC level {ec}, got {dataCodewords.Length}.",
                nameof(dataCodewords));
        }

        var dataBlocks = new byte[blocks][];
        var ecBlocks = new byte[blocks][];
        for (int b = 0; b < blocks; b++)
        {
            var block = new byte[dataPerBlock];
            Array.Copy(dataCodewords, b * dataPerBlock, block, 0, dataPerBlock);
            dataBlocks[b] = block;
            ecBlocks[b] = ReedSolomon.ComputeEc(block, ecPerBlock);
        }

        var result = new List<byte>(Version4.TotalCodewords);

        for (int i = 0; i < dataPerBlock; i++)
        {
            for (int b = 0; b < blocks; b++)
            {
                result.Add(dataBlocks[b][i]);
            }
        }

        for (int i = 0; i < ecPerBlock; i++)
        {
            for (int b = 0; b < blocks; b++)
            {
                result.Add(ecBlocks[b][i]);
            }
        }

        return result.ToArray();
    }
}
