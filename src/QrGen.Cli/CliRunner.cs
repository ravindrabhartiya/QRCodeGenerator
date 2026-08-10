using System;
using System.IO;
using System.Linq;
using QrGen;

namespace QrGen.Cli;

/// <summary>Parses command-line arguments and runs the QR generation pipeline.</summary>
public static class CliRunner
{
    /// <summary>
    /// Runs the CLI with the given arguments, writing results to <paramref name="stdout"/>
    /// and errors to <paramref name="stderr"/>. Returns a process exit code (0 = success).
    /// </summary>
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        string? text = null;
        var ec = EcLevel.M;
        bool dump01 = false;
        string? outPath = null;
        int scale = Renderer.DefaultModuleSize;

        for (int i = 0; i < (args?.Length ?? 0); i++)
        {
            string arg = args![i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    PrintUsage(stdout);
                    return 0;

                case "--dump01":
                    dump01 = true;
                    break;

                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        stderr.WriteLine("error: --out requires a file path.");
                        return 2;
                    }
                    outPath = args[++i];
                    break;

                case "--scale":
                    if (i + 1 >= args.Length)
                    {
                        stderr.WriteLine("error: --scale requires a positive integer.");
                        return 2;
                    }
                    if (!int.TryParse(args[++i], out scale) || scale < 1)
                    {
                        stderr.WriteLine($"error: invalid scale '{args[i]}' (expected a positive integer).");
                        return 2;
                    }
                    break;

                case "--ec":
                    if (i + 1 >= args.Length)
                    {
                        stderr.WriteLine("error: --ec requires a value (L, M, Q, or H).");
                        return 2;
                    }
                    if (!TryParseEc(args[++i], out ec))
                    {
                        stderr.WriteLine(
                            $"error: invalid EC level '{args[i]}' (expected L, M, Q, or H).");
                        return 2;
                    }
                    break;

                default:
                    if (arg.StartsWith('-'))
                    {
                        stderr.WriteLine($"error: unknown option '{arg}'.");
                        return 2;
                    }
                    if (text is not null)
                    {
                        stderr.WriteLine("error: multiple input texts provided.");
                        return 2;
                    }
                    text = arg;
                    break;
            }
        }

        if (text is null)
        {
            stderr.WriteLine("error: no input text provided.");
            PrintUsage(stderr);
            return 2;
        }

        try
        {
            var mode = ModeDetector.Detect(text);
            BitBuffer segment = DataEncoder.EncodeSegment(text, mode);
            byte[] codewords = DataEncoder.Encode(text, mode, ec);
            var (blocks, ecPerBlock, dataPerBlock) = Version4.BlockLayout(ec);
            byte[] finalCodewords = BlockInterleaver.Interleave(codewords, ec);
            QrMatrix matrix = MatrixBuilder.Build(finalCodewords);
            var (maskPattern, maskedMatrix, penalty) = Masking.SelectBestMask(matrix);
            FormatInfo.Place(maskedMatrix, ec, maskPattern);

            if (dump01)
            {
                WriteMatrix01(stdout, maskedMatrix);
                return 0;
            }

            stdout.WriteLine($"Input:      {text}");
            stdout.WriteLine($"Mode:       {mode}");
            stdout.WriteLine("Version:    4");
            stdout.WriteLine($"EC Level:   {ec}");
            stdout.WriteLine($"Char Count: {text.Length}");
            stdout.WriteLine($"Data bits:  {segment.ToBitString()}");
            stdout.WriteLine($"Data codewords ({codewords.Length}):");
            stdout.WriteLine("  " + ToHex(codewords));
            stdout.WriteLine($"Blocks:     {blocks} × ({dataPerBlock} data + {ecPerBlock} EC)");
            stdout.WriteLine($"Final codewords ({finalCodewords.Length}):");
            stdout.WriteLine("  " + ToHex(finalCodewords));
            stdout.WriteLine(
                $"Matrix:     {matrix.Size}×{matrix.Size} " +
                $"({matrix.CountFunction()} function, {matrix.CountData()} data modules)");
            stdout.WriteLine($"Mask:       {maskPattern} (penalty {penalty})");
            stdout.WriteLine($"Format:     {FormatInfo.Compute(ec, maskPattern):X4} (BCH 15,5)");

            if (outPath is not null)
            {
                Renderer.SavePng(maskedMatrix, outPath, scale);
                stdout.WriteLine(
                    $"Saved:      {outPath} " +
                    $"({(maskedMatrix.Size + 2 * Renderer.DefaultQuietZone) * scale}px, " +
                    $"scale {scale}, quiet zone {Renderer.DefaultQuietZone})");
            }

            stdout.WriteLine("Preview (with quiet zone):");
            stdout.Write(Renderer.ToAscii(maskedMatrix));
            return 0;
        }
        catch (ArgumentException ex)
        {
            stderr.WriteLine($"error: {ex.Message}");
            return 1;
        }
        catch (IOException ex)
        {
            stderr.WriteLine($"error: could not write output file: {ex.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            stderr.WriteLine($"error: could not write output file: {ex.Message}");
            return 1;
        }
    }

    private static bool TryParseEc(string value, out EcLevel ec)
    {
        switch (value.ToUpperInvariant())
        {
            case "L": ec = EcLevel.L; return true;
            case "M": ec = EcLevel.M; return true;
            case "Q": ec = EcLevel.Q; return true;
            case "H": ec = EcLevel.H; return true;
            default: ec = EcLevel.M; return false;
        }
    }

    private static string ToHex(byte[] bytes) =>
        string.Join(' ', bytes.Select(b => b.ToString("X2")));

    private static void WriteMatrix01(TextWriter w, QrMatrix m)
    {
        for (int r = 0; r < m.Size; r++)
        {
            var sb = new System.Text.StringBuilder(m.Size);
            for (int c = 0; c < m.Size; c++)
            {
                sb.Append(m.IsDark(r, c) ? '1' : '0');
            }

            w.WriteLine(sb.ToString());
        }
    }

    private static void PrintUsage(TextWriter w)
    {
        w.WriteLine("Usage: qrgen <text> [--ec <L|M|Q|H>] [--out <file.png>] [--scale <n>]");
        w.WriteLine("  Generates a QR Code (Version 4) for the given text.");
        w.WriteLine("  --ec         Error-correction level (default: M).");
        w.WriteLine("  --out        Write a PNG image to the given path.");
        w.WriteLine("  --scale      Pixels per module for --out (default: 8).");
        w.WriteLine("  -h, --help   Show this help.");
    }
}
