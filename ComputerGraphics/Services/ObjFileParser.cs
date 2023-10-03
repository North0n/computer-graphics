﻿using ComputerGraphics.Models;
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
            var faces = new List<List<int>>();
            var normalIndexes = new List<List<int>>();

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
                            var face = new List<int> { int.Parse(indexes[0]) - 1 };
                            var normal = new List<int>{ int.Parse(indexes[2]) - 1 };
                            for (var j = 1; j < 3; ++j)
                            {
                                indexes = argsIndexes[(j + i) % argsIndexes.Count].Split('/');
                                face.Add(int.Parse(indexes[0]) - 1);
                                normal.Add(int.Parse(indexes[2]) - 1);
                            }
                            faces.Add(face);
                            normalIndexes.Add(normal);
                        }
                    }
                }
            }

            return new ObjFileParseResult(vertexes, faces, normals, normalIndexes);
        }
    }
}