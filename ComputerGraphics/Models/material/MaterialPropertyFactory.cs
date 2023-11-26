using System.Numerics;

namespace ComputerGraphics.Models.material;

public class MaterialPropertyFactory
{
    public static IMaterialProperty CreateMaterialProperty(Vector3 value, Texture texture)
    {
        return texture == null ? new DefaultMaterialProperty(value) : new TextureMaterialProperty(texture, value);
    }
}