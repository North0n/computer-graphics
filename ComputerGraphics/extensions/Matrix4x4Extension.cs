using System.Numerics;

namespace ComputerGraphics.extensions;

public static class Matrix4x4Extension
{
    public static Matrix4x4 CreateViewportLeftHanded(float x, float y, float width, float height, float minDepth,
        float maxDepth)
    {
        return new Matrix4x4(
            width / 2    , 0             , 0                  , 0,
            0            , -(height / 2) , 0                  , 0,
            0            , 0             , maxDepth - minDepth, 0,
            x + width / 2, y + height / 2, minDepth           , 1
        );
    }
}