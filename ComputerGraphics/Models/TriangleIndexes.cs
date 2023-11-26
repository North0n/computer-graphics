namespace ComputerGraphics.Models;

public readonly struct TriangleIndexes
{
    public TriangleIndexes(int vertex, int texture, int normal)
    {
        Vertex = vertex;
        Texture = texture;
        Normal = normal;
    }

    public int Vertex { get; }
    public int Normal { get; }
    public int Texture { get; }
}