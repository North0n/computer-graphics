using ComputerGraphics.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace ComputerGraphics.Services
{
    public static class ObjFileParser
    {
        public static ObjFileParseResult Parse(IEnumerable<string> fileContent)
        {
            var vertexes = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<Triangle>();

            foreach (var line in fileContent)
            {
                var args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length > 0)
                {
                    if (args[0] == "v")
                    {
                        var x = float.Parse(args[1], CultureInfo.InvariantCulture);
                        var y = float.Parse(args[2], CultureInfo.InvariantCulture);
                        var z = float.Parse(args[3], CultureInfo.InvariantCulture);
                        vertexes.Add(new(x, y, z));
                    }
                    else if (args[0] == "vn")
                    {
                        var x = float.Parse(args[1], CultureInfo.InvariantCulture);
                        var y = float.Parse(args[2], CultureInfo.InvariantCulture);
                        var z = float.Parse(args[3], CultureInfo.InvariantCulture);
                        normals.Add(new(x, y, z));
                    }
                    else if (args[0] == "f")
                    {
                        var argsIndexes = args.TakeLast(args.Length - 1).ToList();

                        for (var i = 0; i < argsIndexes.Count - 2; ++i)
                        {
                            var indexes = argsIndexes[0].Split('/');
                            var triangleIndexes = new TriangleIndexes[3]
                            {
                                new(int.Parse(indexes[0]) - 1
                                    , int.Parse(indexes[2]) - 1),
                                new(),
                                new()
                            };
                            for (var j = 1; j < 3; ++j)
                            {
                                indexes = argsIndexes[(j + i) % argsIndexes.Count].Split('/');
                                triangleIndexes[j] = new TriangleIndexes(int.Parse(indexes[0]) - 1, int.Parse(indexes[2]) - 1);
                            }
                            triangles.Add(new Triangle(triangleIndexes));
                        }
                    }
                }
            }

            return new ObjFileParseResult(vertexes, normals, triangles);
        }
    }
}