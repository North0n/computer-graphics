using System.Numerics;
using ComputerGraphics.Models.NormalMap;

namespace ComputerGraphics.Models.material;

public class Material
{
    public IMaterialProperty AmbientColor { get; private set; }
    public IMaterialProperty DiffuseColor { get; private set; }
    public IMaterialProperty SpecularColor { get; private set; }
    public float SpecularPower { get; private set; }
    public INormalMap NormalMap { get; private set; }

    public static readonly Vector3 DefaultAmbientValue = new(0.2f);
    public static readonly Vector3 DefaultDiffuseValue = new(0.5f);
    public static readonly Vector3 DefaultSpecularValue = new(0.8f);
    public static readonly Texture DefaultAmbientTexture = null;
    public static readonly Texture DefaultDiffuseTexture = null;
    public static readonly Texture DefaultSpecularTexture = null;
    public static readonly Texture DefaultNormalTexture = null;
    public const float DefaultSpecularPower = 4f;

    public Material(IMaterialProperty ambientColor, IMaterialProperty diffuseColor, IMaterialProperty specularColor,
        float specularPower, INormalMap normalMap)
    {
        AmbientColor = ambientColor;
        DiffuseColor = diffuseColor;
        SpecularColor = specularColor;
        SpecularPower = specularPower;
        NormalMap = normalMap;
    }

    public static Material DefaultMaterial { get; } = new();

    private Material()
    {
        AmbientColor = MaterialPropertyFactory.CreateMaterialProperty(DefaultAmbientValue, DefaultAmbientTexture);
        DiffuseColor = MaterialPropertyFactory.CreateMaterialProperty(DefaultDiffuseValue, DefaultDiffuseTexture);
        SpecularColor = MaterialPropertyFactory.CreateMaterialProperty(DefaultSpecularValue, DefaultSpecularTexture);
        SpecularPower = DefaultSpecularPower;
        NormalMap = null;
    }
}