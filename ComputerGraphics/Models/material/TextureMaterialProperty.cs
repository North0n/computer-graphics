using System.Numerics;

namespace ComputerGraphics.Models.material;

public class TextureMaterialProperty : IMaterialProperty
{
    private readonly Texture _texture;
    private readonly Vector3 _defaultValue;

    public TextureMaterialProperty(Texture texture, Vector3 defaultValue)
    {
        _texture = texture;
        _defaultValue = defaultValue;
    }

    public Vector3 GetValue() => _defaultValue;

    public Vector3 GetValue(float x, float y) => _texture.GetPixel(x, y);
}