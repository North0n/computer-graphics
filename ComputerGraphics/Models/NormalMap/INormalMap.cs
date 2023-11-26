using System.Numerics;

namespace ComputerGraphics.Models.NormalMap;

public interface INormalMap
{
    Vector3 GetNormal(float x, float y);
}