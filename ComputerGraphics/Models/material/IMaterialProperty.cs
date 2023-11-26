using System.Numerics;

namespace ComputerGraphics.Models.material;

public interface IMaterialProperty
{
    Vector3 GetValue();

    Vector3 GetValue(float x, float y);
}