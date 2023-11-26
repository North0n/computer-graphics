using System.Numerics;

namespace ComputerGraphics.extensions;

public static class Vector4Extension
{
    public static Vector3 ToVector3(this Vector4 vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
}