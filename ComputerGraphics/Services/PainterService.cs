using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ComputerGraphics.Models;

namespace ComputerGraphics.Services;

public static class PainterService
{
    private static SpinLock[][] _spinLocks;

    private static int Round(float x)
    {
        return (int)Math.Round(x, MidpointRounding.AwayFromZero);
    }

    private static bool IsBackFace(IReadOnlyList<Vector4> vertexes)
    {
        var a = vertexes[0];
        var b = vertexes[1];
        var c = vertexes[2];

        var ab = b - a;
        var ac = c - a;

        var perpDotProduct = ab.X * ac.Y - ab.Y * ac.X;
        return perpDotProduct > 0;
    }

    private static void DrawLine(float x0, float y0, float x1, float y1, byte r, byte g, byte b, Bgra32Bitmap bitmap)
    {
        // ReSharper disable InconsistentNaming
        var x0i = Round(x0);
        var y0i = Round(y0);
        var x1i = Round(x1);
        var y1i = Round(y1);
        // ReSharper restore InconsistentNaming
        var steps = Math.Max(Math.Abs(x1i - x0i), Math.Abs(y1i - y0i));
        if (steps <= 0)
            return;

        var dx = (float)(x1i - x0i) / steps;
        var dy = (float)(y1i - y0i) / steps;
        float x = x0i;
        float y = y0i;
        for (var i = 0; i < steps; ++i)
        {
            var xi = Round(x);
            var yi = Round(y);
            bitmap.SetPixel(xi, yi, r, g, b);
            x += dx;
            y += dy;
        }
    }

    private static void DrawTriangle(List<Vector4> vertexes, List<Vector4> worldVertexes, List<Vector3> normals,
        Bgra32Bitmap bitmap, float[,] zBuffer, LightSource[] lightSources, Vector3 viewDirection)
    {
        if (IsBackFace(vertexes))
            return;

        var up = vertexes[2];
        var mid = vertexes[1];
        var down = vertexes[0];

        if (down.Y > mid.Y)
            (down, mid) = (mid, down);
        if (down.Y > up.Y)
            (down, up) = (up, down);
        if (mid.Y > up.Y)
            (up, mid) = (mid, up);

        if (down.Y < 0)
            return;

        var upY = (int)up.Y;
        var midY = (int)mid.Y;
        var downY = (int)down.Y;

        var firstSegmentHeight = midY - downY;
        var secondSegmentHeight = upY - midY;

        var totalHeight = upY - downY;
        for (var i = 0; i < totalHeight; i++)
        {
            var y = i + downY;
            if (y >= bitmap.PixelHeight)
                break;

            var secondHalf = i > firstSegmentHeight || midY == downY;
            var segmentHeight = secondHalf ? secondSegmentHeight : firstSegmentHeight;

            var alpha = (float)i / totalHeight;
            var a = down + (up - down) * alpha;

            var beta = (float)(i - (secondHalf ? firstSegmentHeight : 0)) / segmentHeight;
            var b = secondHalf ? mid + (up - mid) * beta : down + (mid - down) * beta;

            if (a.X > b.X)
                (a, b) = (b, a);

            var deltaX = b.X - a.X + 1;

            for (var x = (int)a.X; x <= (int)b.X; x++)
            {
                if (x >= bitmap.PixelWidth || x < 0)
                    break;

                var p = (x - a.X) / deltaX;

                var z = a.Z + p * (b.Z - a.Z);

                var (red, green, blue) = GetPointColor(new Vector3(x, y, z), vertexes, normals, worldVertexes,
                    lightSources, viewDirection);
                var gotLock = false;
                try
                {
                    _spinLocks[x][y].Enter(ref gotLock);
                    if (zBuffer[x, y] > z)
                    {
                        zBuffer[x, y] = z;
                        bitmap.SetPixel(x, y, red, green, blue);
                    }
                }
                finally
                {
                    if (gotLock) _spinLocks[x][y].Exit();
                }
            }
        }
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

    private const float AmbientLightIntensity = 0.1f;
    private static readonly Vector3 ModelColor = new(0.8f, 0.2f, 0.8f);

    private static Vector3 GetDiffusePlusSpecular(LightSource lightSource, Vector3 worldPos,
        Vector3 interpolatedNormal, Vector3 viewDirection)
    {
        var lightDirection = lightSource.Position - worldPos;
        var lightDistSqr = lightDirection.LengthSquared();
        var normLightDir = Vector3.Normalize(lightDirection);
        var nDotL = Math.Max(0, Vector3.Dot(interpolatedNormal, normLightDir));
        var irradiance = lightSource.Color * lightSource.Intensity * nDotL / lightDistSqr;
        var diffuse = irradiance * ModelColor;
        var rDotV = Math.Max(0, Vector3.Dot(viewDirection, Vector3.Reflect(normLightDir, interpolatedNormal)));
        var specular = MathF.Pow(rDotV, 64) * ModelColor * irradiance;

        return diffuse + specular;
    }

    private static Vector3 GetInterpolatedWorldVertex(List<Vector4> vertexes, List<Vector4> worldVertexes, int x, int y,
        float z)
    {
        var v1 = vertexes[0];
        var v2 = vertexes[1];
        var v3 = vertexes[2];

        var vx = new Vector3(v3.X - v1.X, v2.X - v1.X, v1.X - x);
        var vy = new Vector3(v3.Y - v1.Y, v2.Y - v1.Y, v1.Y - y);

        var k = Vector3.Cross(vx, vy);
        var k1 = 1 - (k.X + k.Y) / k.Z;
        var k2 = k.Y / k.Z;
        var k3 = k.X / k.Z;

        var kp1 = k1 / v1.Z * z;
        var kp2 = k2 / v2.Z * z;
        var kp3 = k3 / v3.Z * z;

        var res = worldVertexes[0] * kp1 + worldVertexes[1] * kp2 + worldVertexes[2] * kp3;
        return new Vector3(res.X, res.Y, res.Z);
    }

    private static (float R, float G, float B) GetPointColor(Vector3 point, List<Vector4> vertexes, List<Vector3> normals,
        List<Vector4> worldVertexes, LightSource[] lightSources, Vector3 viewDirection)
    {
        var a = new Vector3(vertexes[0].X, vertexes[0].Y, vertexes[0].Z);
        var b = new Vector3(vertexes[1].X, vertexes[1].Y, vertexes[1].Z);
        var c = new Vector3(vertexes[2].X, vertexes[2].Y, vertexes[2].Z);

        var area = Vector3.Cross(b - a, c - a).Length();

        var u = Vector3.Cross(c - b, point - b).Length() / area;
        var v = Vector3.Cross(a - c, point - c).Length() / area;
        var w = Vector3.Cross(b - a, point - a).Length() / area;

        var worldPos = GetInterpolatedWorldVertex(vertexes, worldVertexes, (int)point.X, (int)point.Y, point.Z);
        var interpolatedNormal = Vector3.Normalize(u * normals[0] + v * normals[1] + w * normals[2]);
        var ambient = AmbientLightIntensity * new Vector3(0.5f, 0.5f, 0.5f) * ModelColor;

        var sum = lightSources.Aggregate(Vector3.Zero,
            (current, lightSource) =>
                current + GetDiffusePlusSpecular(lightSource, worldPos, interpolatedNormal, viewDirection));
        var color = LinearToSrgb(AcesFilm(ambient + sum));

        return (Math.Max(0, Math.Min(color.X, 1)),
            Math.Max(0, Math.Min(color.Y, 1)),
            Math.Max(0, Math.Min(color.Z, 1)))  ;
    }

    public static Bgra32Bitmap DrawModel(Vector4[] vertexes, Vector4[] worldVertexes, Vector3[] normals,
        List<Triangle> triangles, int width, int height, float[,] zBuffer, LightSource[] lightSources,
        Vector3 viewDirection)
    {
        InitializeSpinLocks(width, height);
        
        for (var i = 0; i < width; ++i)
        {
            for (var j = 0; j < height; ++j)
            {
                zBuffer[i, j] = float.MaxValue;
            }
        }

        Bgra32Bitmap bitmap = new(width, height);
        bitmap.Source.Lock();

        Parallel.ForEach(Partitioner.Create(0, triangles.Count), range =>
        {
            for (var j = range.Item1; j < range.Item2; ++j)
            {
                var idxs = triangles[j].Indexes;
                DrawTriangle(idxs.Select(idx => vertexes[idx.Vertex]).ToList(),
                    idxs.Select(idx => worldVertexes[idx.Vertex]).ToList(),
                    idxs.Select(idx => normals[idx.Normal]).ToList(), bitmap, zBuffer, lightSources, viewDirection);
            }
        });

        bitmap.Source.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Source.Unlock();
        return bitmap;
    }

    private static void DrawCircle(int x, int y, int radius, byte r, byte g, byte b, Bgra32Bitmap bitmap)
    {
        for (int i = 0; i <= radius * 2; i++)
        {
            for (int j = 0; j <= radius * 2; j++)
            {
                int dx = i - radius;
                int dy = j - radius;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    int pixelX = x - radius + i;
                    int pixelY = y - radius + j;
                    if (pixelX >= 0 && pixelX < bitmap.PixelWidth && pixelY >= 0 && pixelY < bitmap.PixelHeight)
                    {
                        bitmap.SetPixel(pixelX, pixelY, r, g, b);
                    }
                }
            }
        }
    }

    private static void InitializeSpinLocks(int width, int height)
    {
        if (_spinLocks != null && _spinLocks.Length != 0
            && _spinLocks.Length == width && _spinLocks[0].Length == height)
        {
            return;
        }

        _spinLocks = Enumerable.Repeat(new SpinLock[height], width).ToArray();
    }

    public static void AddMinimapToBitmap(ImageInfo positions, Bgra32Bitmap bitmap)
    {
        const int mapHeight = 300;
        const int mapWidth = 300;
        const int borderDistance = 10;

        var mapX = bitmap.PixelWidth - mapWidth - borderDistance;

        var startX = mapX + mapWidth / 2;
        const int startY = borderDistance + mapHeight / 2;

        const int pixelsInHorizontalAxis = 200;
        const int pixelsInVerticalAxis = 200;

        const float horizontalProportion = (float)mapWidth / pixelsInHorizontalAxis;
        const float verticalProportion = (float)mapHeight / pixelsInVerticalAxis;

        var objectX = (int)Math.Round(startX + horizontalProportion * positions.PositionX);
        var objectY = (int)Math.Round(startY + verticalProportion * positions.PositionZ);

        var camPosition = VertexTransformer.ToOrthogonal(positions.CameraPosition, positions.CameraTarget);
        var cameraX = (int)Math.Round(startX + horizontalProportion * camPosition.X);
        var cameraY = (int)Math.Round(startY + verticalProportion * camPosition.Z);

        DrawLine(mapX, borderDistance, mapX + mapWidth, borderDistance, 0, 0, 0, bitmap); // top left to top right
        DrawLine(mapX + mapWidth, borderDistance, mapX + mapWidth, borderDistance + mapHeight, 0, 0, 0,
            bitmap); // top right to bottom right
        DrawLine(mapX + mapWidth, borderDistance + mapHeight, mapX, borderDistance + mapHeight, 0, 0, 0,
            bitmap); // bottom right to bottom left
        DrawLine(mapX, borderDistance + mapHeight, mapX, borderDistance, 0, 0, 0, bitmap); // bottom left to top left

        // cross
        DrawLine(mapX + mapWidth / 2.0f, borderDistance, mapX + mapWidth / 2.0f, borderDistance + mapHeight, 0, 0, 0,
            bitmap);
        DrawLine(mapX, borderDistance + mapHeight / 2.0f, mapX + mapWidth, borderDistance + mapHeight / 2.0f, 0, 0, 0,
            bitmap);

        DrawCircle(objectX, objectY, 5, 1, 0, 0, bitmap);
        DrawCircle(cameraX, cameraY, 5, 0, 1, 0, bitmap);
    }
}