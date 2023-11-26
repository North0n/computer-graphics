using ComputerGraphics.Models.material;

namespace ComputerGraphics.Models.NormalMap;

public class NormalMapFactory
{
    public static INormalMap CreateNormalMap(Texture normalMap) =>
        normalMap == null ? DefaultNormalMap.GetInstance() : new NormalMap(normalMap);
}