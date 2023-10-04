namespace ComputerGraphics.Models;

public readonly struct TriangleIndexes
{
    public TriangleIndexes(int vertex, int normal)
    {
        Vertex = vertex;
        Normal = normal;
    }

    public int Vertex { get; }
    public int Normal { get; }
}