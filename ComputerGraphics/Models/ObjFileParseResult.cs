using System.Collections.Generic;
using System.Numerics;

namespace ComputerGraphics.Models
{
    public record ObjFileParseResult(List<Vector3> Vertexes, List<List<int>> Faces);
}