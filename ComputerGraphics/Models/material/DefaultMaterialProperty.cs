using System.Numerics;

namespace ComputerGraphics.Models.material;

public class DefaultMaterialProperty : IMaterialProperty
{
    private readonly Vector3 _value;

    public DefaultMaterialProperty(Vector3 value)
    {
        _value = value;
    }

    public Vector3 GetValue() => _value;

    public Vector3 GetValue(float x, float y) => _value;
}