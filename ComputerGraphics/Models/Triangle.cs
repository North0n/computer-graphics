using ComputerGraphics.Models.material;

namespace ComputerGraphics.Models;

public struct Triangle
{
    public Triangle(TriangleIndexes[] indexes, Material material)
    {
        Indexes = indexes;
        Material = material;
    }

    public TriangleIndexes[] Indexes { get; }
    public Material Material { get; }
}