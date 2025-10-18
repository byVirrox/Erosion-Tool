using System;

/// <summary>
/// Represents a 2D integer coordinate in a grid.
/// This struct is engine-agnostic.
/// </summary>
public struct GridCoordinates : System.IEquatable<GridCoordinates>
{
    public readonly int X;
    public readonly int Y;

    public GridCoordinates(int x, int y) { X = x; Y = y; }

    public bool Equals(GridCoordinates other)
    {
        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        return (X * 397) ^ Y;
    }

    public override bool Equals(object obj)
    {
        return obj is GridCoordinates other && Equals(other);
    }

    public override string ToString()
    {
        return (X.ToString() + ", " + Y.ToString());
    }
}
