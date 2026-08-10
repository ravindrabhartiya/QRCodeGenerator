using System;

namespace QrGen;

/// <summary>
/// A square grid of QR modules. Each module is either dark or light, and is either a
/// function module (finder/timing/alignment/format/etc.) or a data module.
/// </summary>
public sealed class QrMatrix
{
    private readonly bool[,] _dark;
    private readonly bool[,] _function;

    /// <summary>Creates an all-light matrix with no function modules.</summary>
    /// <param name="size">Side length in modules (33 for Version 4).</param>
    public QrMatrix(int size)
    {
        if (size < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive.");
        }

        Size = size;
        _dark = new bool[size, size];
        _function = new bool[size, size];
    }

    /// <summary>Side length of the matrix in modules.</summary>
    public int Size { get; }

    /// <summary>Returns whether the module at the given position is dark.</summary>
    public bool IsDark(int row, int col) => _dark[row, col];

    /// <summary>Returns whether the module at the given position is a function module.</summary>
    public bool IsFunction(int row, int col) => _function[row, col];

    /// <summary>Sets a function module to the given colour and marks it reserved.</summary>
    public void SetFunction(int row, int col, bool dark)
    {
        _dark[row, col] = dark;
        _function[row, col] = true;
    }

    /// <summary>Sets a data module's colour without marking it as a function module.</summary>
    public void SetData(int row, int col, bool dark)
    {
        _dark[row, col] = dark;
    }

    /// <summary>Counts the function (reserved) modules in the matrix.</summary>
    public int CountFunction()
    {
        int count = 0;
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                if (_function[r, c])
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>Counts the data (non-function) modules in the matrix.</summary>
    public int CountData() => (Size * Size) - CountFunction();

    /// <summary>Counts the dark modules in the matrix.</summary>
    public int CountDark()
    {
        int count = 0;
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                if (_dark[r, c])
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>Creates a deep copy of this matrix, preserving colour and function state.</summary>
    public QrMatrix Clone()
    {
        var copy = new QrMatrix(Size);
        Array.Copy(_dark, copy._dark, _dark.Length);
        Array.Copy(_function, copy._function, _function.Length);
        return copy;
    }
}
