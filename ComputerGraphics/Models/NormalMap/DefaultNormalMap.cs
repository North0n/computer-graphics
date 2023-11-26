using System.Numerics;

namespace ComputerGraphics.Models.NormalMap;

public class DefaultNormalMap : INormalMap
{
    private static DefaultNormalMap _instance = new();

    public static DefaultNormalMap GetInstance() => _instance;

    public Vector3 GetNormal(float x, float y) => Vector3.One;

    private DefaultNormalMap()
    {
    }
}