﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ComputerGraphics.extensions;
using ComputerGraphics.Models;

namespace ComputerGraphics.Services;

public static class ColorService
{
    public static (float R, float G, float B) GetColor(Vector3 point, IReadOnlyList<Vector4> vertexes,
        IReadOnlyList<Vector3> normals, IReadOnlyList<Vector3> textures, IReadOnlyList<Vector4> worldVertexes,
        IEnumerable<LightSource> lightSources, Vector3 viewDirection, Triangle triangle)
    {
        var barycentric = GetBarycentricCoordinates(vertexes, worldVertexes, (int)point.X, (int)point.Y, point.Z, triangle);
        var worldPos = worldVertexes[triangle.Indexes[0].Vertex] * barycentric.X +
                       worldVertexes[triangle.Indexes[1].Vertex] * barycentric.Y +
                       worldVertexes[triangle.Indexes[2].Vertex] * barycentric.Z;
        var texture = barycentric.X * textures[triangle.Indexes[0].Texture] +
                      barycentric.Y * textures[triangle.Indexes[1].Texture] +
                      barycentric.Z * textures[triangle.Indexes[2].Texture];

        var x = texture.X;
        var y = texture.Y;
        var material = triangle.Material;
        var normal = material.NormalMap?.GetNormal(x, y) ?? Vector3.Normalize(
            barycentric.X * normals[triangle.Indexes[0].Normal] +
            barycentric.Y * normals[triangle.Indexes[1].Normal] +
            barycentric.Z * normals[triangle.Indexes[2].Normal]);
        var mrao = material.MRAOColor.GetValue(x, y);

        var ambient = AmbientLightIntensity * ModelAmbientConsumption * material.AmbientColor.GetValue(x, y);
        var sum = lightSources.Aggregate(Vector3.Zero,
            (current, lightSource) =>
                current + GetDiffusePlusSpecular(lightSource, worldPos.ToVector3(), normal, viewDirection,
                    SrgbToLinear(material.DiffuseColor.GetValue(x, y)),
                    SrgbToLinear(material.SpecularColor.GetValue(x, y)),
                    material.SpecularPower, mrao));
        var linearColor = ambient + sum;
        var keColor = material.KeColor.GetValue(x, y);
        linearColor += 10 * keColor;
        var color = LinearToSrgb(AcesFilm(linearColor));

        return (Math.Max(0, Math.Min(color.X, 1)),
            Math.Max(0, Math.Min(color.Y, 1)),
            Math.Max(0, Math.Min(color.Z, 1)));
    }

    private static Vector3 AcesFilm(Vector3 color)
    {
        color = new(Vector3.Dot(new(0.59719f, 0.35458f, 0.04823f), color),
            Vector3.Dot(new(0.07600f, 0.90834f, 0.01566f), color),
            Vector3.Dot(new(0.02840f, 0.13383f, 0.83777f), color));

        color = (color * (color + new Vector3(0.0245786f)) - new Vector3(0.000090537f)) /
                (color * (0.983729f * color + new Vector3(0.4329510f)) + new Vector3(0.238081f));

        color = new(Vector3.Dot(new(1.60475f, -0.53108f, -0.07367f), color),
            Vector3.Dot(new(-0.10208f, 1.10813f, -0.00605f), color),
            Vector3.Dot(new(-0.00327f, -0.07276f, 1.07602f), color));

        return Vector3.Clamp(color, Vector3.Zero, Vector3.One);
    }

    private static Vector3 Pow(Vector3 color, float x)
    {
        return new Vector3(MathF.Pow(color.X, x), MathF.Pow(color.Y, x), MathF.Pow(color.Z, x));
    }

    private static Vector3 LinearToSrgb(Vector3 color)
    {
        static float LinearToSrgb(float c) =>
            c <= 0.0031308f ? 12.92f * c : 1.055f * MathF.Pow(c, 1 / 2.4f) - 0.055f;

        return new(LinearToSrgb(color.X), LinearToSrgb(color.Y), LinearToSrgb(color.Z));
    }

    private static Vector3 SrgbToLinear(Vector3 color)
    {
        static float SrgbToLinear(float c) =>
            c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

        return new(SrgbToLinear(color.X), SrgbToLinear(color.Y), SrgbToLinear(color.Z));
    }

    private const float AmbientLightIntensity = 0.01f;
    private static readonly Vector3 ModelAmbientConsumption = new(0.5f, 0.5f, 0.5f);

    private static Vector3 GetDiffusePlusSpecular(LightSource lightSource, Vector3 worldPos,
        Vector3 interpolatedNormal, Vector3 viewDirection, Vector3 diffuseColor, Vector3 specularColor,
        float specularPower, Vector3 mrao)
    {
        var metalness = mrao.X;

        var lightDirection = lightSource.Position - worldPos;
        var lightDistSqr = lightDirection.LengthSquared();
        var normLightDir = Vector3.Normalize(lightDirection);
        var nDotL = Math.Max(0, Vector3.Dot(interpolatedNormal, normLightDir));
        var irradiance = lightSource.Color * lightSource.Intensity * nDotL / lightDistSqr;
        var diffuse = irradiance * diffuseColor * (1.0f - metalness);
        var rDotV = Math.Max(0, Vector3.Dot(viewDirection, Vector3.Reflect(normLightDir, interpolatedNormal)));
        var specular = MathF.Pow(rDotV, specularPower) * specularColor * irradiance;

        return diffuse + specular;
    }

    private static Vector3 GetBarycentricCoordinates(IReadOnlyList<Vector4> vertexes,
        IReadOnlyList<Vector4> worldVertexes, int x, int y, float z, Triangle triangle)
    {
        var v1 = vertexes[triangle.Indexes[0].Vertex];
        var v2 = vertexes[triangle.Indexes[1].Vertex];
        var v3 = vertexes[triangle.Indexes[2].Vertex];
        var wv1 = worldVertexes[triangle.Indexes[0].Vertex];
        var wv2 = worldVertexes[triangle.Indexes[1].Vertex];
        var wv3 = worldVertexes[triangle.Indexes[2].Vertex];

        var vx = new Vector3(v3.X - v1.X, v2.X - v1.X, v1.X - x);
        var vy = new Vector3(v3.Y - v1.Y, v2.Y - v1.Y, v1.Y - y);

        var k = Vector3.Cross(vx, vy);
        if (k.Z == 0)
            k.Z = 1;
        var k1 = (1 - (k.X + k.Y) / k.Z) / wv1.W;
        var k2 = k.Y / k.Z / wv2.W;
        var k3 = k.X / k.Z / wv3.W;

        var kSum = k1 + k2 + k3;
        var z0 = 1 / (kSum == 0 ? 1 : kSum);

        var kp1 = k1 * z0;
        var kp2 = k2 * z0;
        var kp3 = k3 * z0;

        return new Vector3(kp1, kp2, kp3);
    }
}