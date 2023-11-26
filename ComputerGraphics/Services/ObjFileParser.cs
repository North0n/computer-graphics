using ComputerGraphics.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using ComputerGraphics.Models.material;
using ComputerGraphics.Models.NormalMap;

namespace ComputerGraphics.Services
{
    public static class ObjFileParser
    {
        public static ObjFileParseResult Parse(string objPath)
        {
            var fileContent = File.ReadLines(objPath);
            var vertexes = new List<Vector3>();
            var textures = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<Triangle>();
            var materials = new Dictionary<string, Material>();
            var currentMaterial = Material.DefaultMaterial;

            foreach (var line in fileContent)
            {
                var args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length > 0)
                {
                    if (args[0] == "v")
                    {
                        vertexes.Add(ParseVector(args));
                    }
                    else if (args[0] == "vn")
                    {
                        normals.Add(ParseVector(args));
                    }
                    else if (args[0] == "vt")
                    {
                        var texture = new Vector3(
                            args.Length > 1 ? ParseFloat(args[1]) : 0,
                            args.Length > 2 ? ParseFloat(args[2]) : 0,
                            args.Length > 3 ? ParseFloat(args[3]) : 0
                        );
                        textures.Add(texture);
                    }
                    else if (args[0] == "f")
                    {
                        var argsIndexes = args.TakeLast(args.Length - 1).ToList();

                        for (var i = 0; i < argsIndexes.Count - 2; ++i)
                        {
                            var indexes = argsIndexes[0].Split('/');
                            var triangleIndexes = new TriangleIndexes[]
                            {
                                new(indexes.Length > 0 ? int.Parse(indexes[0]) - 1 : -1,
                                    indexes.Length > 1 ? int.Parse(indexes[1]) - 1 : -1,
                                    indexes.Length > 2 ? int.Parse(indexes[2]) - 1 : -1),
                                new(),
                                new()
                            };
                            for (var j = 1; j < 3; ++j)
                            {
                                indexes = argsIndexes[(j + i) % argsIndexes.Count].Split('/');
                                triangleIndexes[j] = new TriangleIndexes(
                                    indexes.Length > 0 ? int.Parse(indexes[0]) - 1 : -1,
                                    indexes.Length > 1 ? int.Parse(indexes[1]) - 1 : -1,
                                    indexes.Length > 2 ? int.Parse(indexes[2]) - 1 : -1);
                            }
                            triangles.Add(new Triangle(triangleIndexes, currentMaterial));
                        }
                    }
                    else if (args[0] == "usemtl")
                    {
                        var materialName = args[1];
                        currentMaterial = materials.TryGetValue(materialName, out var material)
                            ? material
                            : Material.DefaultMaterial;
                    }
                    else  if (args[0] == "mtllib")
                    {
                        var path = Path.GetDirectoryName(objPath);
                        path += "\\" + args[1];
                        var newMaterials = ParseMaterials(path);
                        foreach(var matName in newMaterials.Keys)
                        {
                            materials[matName] = newMaterials[matName];
                        }
                    }
                }
            }

            return new ObjFileParseResult(vertexes, textures, normals, triangles);
        }

        private static Dictionary<string, Material> ParseMaterials(string mtlPath)
        {
            var materials = new Dictionary<string, Material>();
            if (!File.Exists(mtlPath))
            {
                Debug.Assert(false);
                return materials;
            }

            var fileContent = File.ReadLines(mtlPath);
            string materialName = null;

            var ambientValue = Material.DefaultAmbientValue;
            var diffuseValue = Material.DefaultDiffuseValue;
            var specularValue = Material.DefaultSpecularValue;
            var ambientTexture = Material.DefaultAmbientTexture;
            var diffuseTexture = Material.DefaultDiffuseTexture;
            var specularTexture = Material.DefaultSpecularTexture;
            var normalTexture = Material.DefaultNormalTexture;
            var specularPower = Material.DefaultSpecularPower;

            foreach (var line in fileContent)
            {
                var args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length <= 0)
                    continue;

                if (args[0] == "newmtl")
                {
                    // Add previous material
                    if (materialName != null)
                    {
                        var ambientProperty =
                            MaterialPropertyFactory.CreateMaterialProperty(ambientValue, ambientTexture);
                        var diffuseProperty =
                            MaterialPropertyFactory.CreateMaterialProperty(diffuseValue, diffuseTexture);
                        var specularProperty =
                            MaterialPropertyFactory.CreateMaterialProperty(specularValue, specularTexture);
                        var normalMap = NormalMapFactory.CreateNormalMap(normalTexture);

                        materials[materialName] = new Material(ambientProperty, diffuseProperty, specularProperty,
                            specularPower, normalMap);

                        ambientValue = Material.DefaultAmbientValue;
                        diffuseValue = Material.DefaultDiffuseValue;
                        specularValue = Material.DefaultSpecularValue;
                        ambientTexture = Material.DefaultAmbientTexture;
                        diffuseTexture = Material.DefaultDiffuseTexture;
                        specularTexture = Material.DefaultSpecularTexture;
                        normalTexture = Material.DefaultNormalTexture;
                        specularPower = Material.DefaultSpecularPower;
                    }

                    // Save name of new material
                    materialName = args[1];
                }
                else if (args[0] == "Ka")
                {
                    ambientValue = ParseVector(args);
                }
                else if (args[0] == "Kd")
                {
                    diffuseValue = ParseVector(args);
                }
                else if (args[0] == "Ks")
                {
                    specularValue = ParseVector(args);
                }
                else if (args[0] == "Ns")
                {
                    specularPower = ParseFloat(args[0]);
                }
                else if (args[0] == "map_Ka")
                {
                    ambientTexture = ParseTexture(mtlPath, args[1]);
                }
                else if (args[0] == "map_Kd")
                {
                    diffuseTexture = ParseTexture(mtlPath, args[1]);
                }
                else if (args[0] == "map_Ks")
                {
                    specularTexture = ParseTexture(mtlPath, args[1]);
                }
                else if (args[0] == "map_bump" || args[0] == "bump")
                {
                    normalTexture = ParseTexture(mtlPath, args[1]);
                }
            }

            // Add previous material
            if (materialName != null)
            {
                var ambientProperty =
                    MaterialPropertyFactory.CreateMaterialProperty(ambientValue, ambientTexture);
                var diffuseProperty =
                    MaterialPropertyFactory.CreateMaterialProperty(diffuseValue, diffuseTexture);
                var specularProperty =
                    MaterialPropertyFactory.CreateMaterialProperty(specularValue, specularTexture);
                var normalMap = NormalMapFactory.CreateNormalMap(normalTexture);

                materials[materialName] = new Material(ambientProperty, diffuseProperty, specularProperty,
                    specularPower, normalMap);
            }

            return materials;
        }

        private static Vector3 ParseVector(string[] args)
        {
            return new Vector3(
                ParseFloat(args[1]),
                ParseFloat(args[2]),
                ParseFloat(args[3])
            );
        }

        private static float ParseFloat(string str)
        {
            return float.Parse(str, CultureInfo.InvariantCulture);
        }

        private static Texture ParseTexture(string materialPath, string texturePath) =>
            new(@$"{Path.GetDirectoryName(materialPath)}\{texturePath}");
    }
}