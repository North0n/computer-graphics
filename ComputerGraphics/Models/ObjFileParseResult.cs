using System.Collections.Generic;
using System.Numerics;

namespace ComputerGraphics.Models
{
    public record ObjFileParseResult(List<Vector3> Vertexes, List<Vector3> Textures, List<Vector3> Normals,
        List<Triangle> Triangles);
}