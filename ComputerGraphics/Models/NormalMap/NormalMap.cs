using System.Numerics;
using ComputerGraphics.Models.material;

namespace ComputerGraphics.Models.NormalMap;

public class NormalMap : INormalMap
{
    private readonly Texture _normalMap;

    public NormalMap(Texture normalMap)
    {
        _normalMap = normalMap;
    }

    public Vector3 GetNormal(float x, float y) => Vector3.Normalize(_normalMap.GetPixel(x, y) * 2 - Vector3.One);
}