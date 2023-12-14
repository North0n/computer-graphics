using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ComputerGraphics.extensions;
using ComputerGraphics.Models;

namespace ComputerGraphics.Services;

public static class ColorService
{
    private static readonly Vector3 AmbientWeight = new(0.1f);

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

        var albedo = SrgbToLinear(material.DiffuseColor.GetValue(x, y));
        var emissivity = SrgbToLinear(material.Emissivity.GetValue(x, y));
        var mrao = material.MRAOColor.GetValue(x, y);
        
        var ambient = AmbientWeight * albedo * mrao.Z;

        var sum = lightSources.Aggregate(
            Vector3.Zero,
            (current, lightSource) => current + GetPhysicallyBasedRenderingLight(
                lightSource,
                viewDirection,
                normal,
                worldPos.ToVector3(),
                mrao,
                albedo
            )
        );

        var linearColor = ambient + sum + emissivity * 10;
        
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

    private static Vector3 GetPhysicallyBasedRenderingLight(LightSource lightSource, Vector3 viewDirection,
        Vector3 normal, Vector3 worldPos, Vector3 mrao, Vector3 albedo)
    {
        var distance = Vector3.Distance(worldPos, lightSource.Position);
        var lightDirection = Vector3.Normalize(lightSource.Position - worldPos);
        var halfwayVector = Vector3.Normalize(viewDirection + lightDirection);
        var attenuation = 1.0f / (distance * distance);

        var metallic = mrao.X;
        var roughness = mrao.Y;

        var nDotH = MathF.Max(Vector3.Dot(normal, halfwayVector), 1e-10f);
        var nDotV = MathF.Max(Vector3.Dot(normal, viewDirection), 1e-10f);
        var nDotL = MathF.Max(Vector3.Dot(normal, lightDirection), 1e-10f);

        var f0 = Vector3.Lerp(new Vector3(0.04f), albedo, metallic);

        var lambert = albedo / MathF.PI;

        // Cook-Torrance
        var d = D_GGX(roughness, nDotH);
        var g = G_Smith(nDotV, nDotL, roughness);
        var f = F_Schlick(nDotV, f0);

        var ks = f;
        var kd = (Vector3.One - ks) * (1 - metallic);

        Vector3 cookTorranceNumerator = d * g * f;
        float cookTorranceDenominator = 4.0f * nDotV * nDotL;

        Vector3 cookTorrance = cookTorranceNumerator / cookTorranceDenominator;
        Vector3 brdf = kd * lambert + cookTorrance;

        Vector3 result = brdf * lightSource.Color * lightSource.Intensity * nDotL * attenuation;

        return result;
    }

    private static Vector3 F_Schlick(float hDotV, Vector3 f0)
    {
        return f0 + (Vector3.One - f0) * MathF.Pow(1 - hDotV, 5);
    }

    // Smith Model
    private static float G_Smith(float nDotV, float nDotL, float roughness)
    {
        var k = (float)Math.Pow(roughness, 2) / 2;

        return GetFactor(nDotV, k) * GetFactor(nDotL, k);
    }

    // GGX/Trowbridge-Reitz Normal Distribution Function
    private static float D_GGX(float roughness, float nDotH)
    {
        var a2 = (float)Math.Pow(roughness, 4);

        var denominator = nDotH * nDotH * (a2 - 1.0f) + 1.0f;
        denominator = Math.Max(MathF.PI * denominator * denominator, 1e-12f);

        return a2 / denominator;
    }

    // Schlick-Beckman Geometry Shadowing Function
    private static float GetFactor(float dot, float k)
    {
        var denominator = MathF.Max(1e-10f, dot * (1 - k) + k);
        return dot / denominator;
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