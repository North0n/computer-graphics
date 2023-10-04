namespace ComputerGraphics.Models;

public struct Triangle
{
    public Triangle(TriangleIndexes[] indexes)
    {
        Indexes = indexes;
    }

    public TriangleIndexes[] Indexes { get; }
}