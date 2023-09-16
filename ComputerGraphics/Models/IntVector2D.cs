using System;

namespace ComputerGraphics.Models;

public readonly struct IntVector2D : IComparable<IntVector2D>
{
    public IntVector2D(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }

    public int CompareTo(IntVector2D other)
    {
        if (Y < other.Y)
            return -1;

        if (Y.CompareTo(other.Y) == 0)
            return X.CompareTo(other.X);

        return 1;
    }
}