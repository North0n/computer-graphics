using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using ComputerGraphics.Models;

namespace ComputerGraphics.Services;

public static class PainterService
{
    private static int Round(float x)
    {
        return (int)Math.Round(x, MidpointRounding.AwayFromZero);
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

    private static void DrawPolygon(IReadOnlyList<Vector4> vertexes, List<Vector3> normals, Bgra32Bitmap bitmap,
        float[,] zBuffer, Vector3 lightDirection)
    {
        var minY = Round(vertexes.Min(v => v.Y));
        var maxY = Round(vertexes.Max(v => v.Y));

        var intersections = new PriorityQueue<IntVector2D, IntVector2D>((maxY - minY) * vertexes.Count / 2);
        for (var y = minY; y <= maxY; ++y)
        {
            for (var j = 0; j < vertexes.Count; j++)
            {
                var vertex = vertexes[j];
                var nextVertex = vertexes[(j + 1) % vertexes.Count];

                // If vertexes are on the one side of the line skip them
                if (y < nextVertex.Y ^ y > vertex.Y)
                    continue;

                // Find intersection point
                var k = (nextVertex.Y - vertex.Y) / (nextVertex.X - vertex.X);
                var x = float.IsFinite(k) ? (y - (nextVertex.Y - k * nextVertex.X)) / k : vertex.X;

                // Don't add intersection point if it is not inside polygon
                if (x < Math.Min(vertex.X, nextVertex.X) || x > Math.Max(vertex.X, nextVertex.X))
                    continue;

                var len = Math.Sqrt(Math.Pow(vertex.X - nextVertex.X, 2) + Math.Pow(vertex.Y - nextVertex.Y, 2));
                var curLen = Math.Sqrt(Math.Pow(vertex.X - x, 2) + Math.Pow(vertex.Y - y, 2));
                var z = (float)(curLen / len * (nextVertex.Z - vertex.Z) + vertex.Z);
                var vec = new IntVector2D(Round(x), y, z);
                intersections.Enqueue(vec, vec);
            }
        }

        var intensity = normals.ConvertAll(n => Vector3.Dot(n, -lightDirection)).Average();
        // intensity = Math.Abs(intensity);
        if (intensity < 0)
            return;

        var (r, g, b) = ((byte)(intensity * 200), (byte)(intensity * 200), (byte)(intensity * 200));
        while (intersections.Count >= 2)
        {
            var vec1 = intersections.Dequeue();
            var vec2 = intersections.Dequeue();

            for (var x = vec1.X; x <= vec2.X; ++x)
            {
                if (x >= zBuffer.GetLength(0))
                    break;

                if (x <= 0 || vec1.Y <= 0 || vec1.Y >= zBuffer.GetLength(1))
                    continue;

                var k = vec2.X - vec1.X != 0 ? (float)(x - vec1.X) / (vec2.X - vec1.X) : float.MaxValue;

                var z = k * (vec2.Z - vec1.Z) + vec1.Z;
                if (zBuffer[x, vec1.Y] >= z)
                    continue;

                zBuffer[x, vec1.Y] = z;
                bitmap.SetPixel(x, vec1.Y, r, g, b);
            }
        }
    }

    public static Bgra32Bitmap DrawModel(Vector4[] vertexes, List<List<int>> faces, int width, int height,
        float[,] zBuffer, Vector3[] normals, List<List<int>> normalIndexes, Vector3 lightDirection)
    {
        for (var i = 0; i < width; ++i)
        {
            for (var j = 0; j < height; ++j)
            {
                zBuffer[i, j] = float.MinValue;
            }
        }

        Bgra32Bitmap bitmap = new(width, height);
        bitmap.Source.Lock();

        Parallel.ForEach(Partitioner.Create(0, faces.Count), range =>
        {
            for (var j = range.Item1; j < range.Item2; ++j)
            {
                var face = faces[j];
                var curNormals = normalIndexes[j];
                DrawPolygon(face.ConvertAll(idx => vertexes[idx]), curNormals.ConvertAll(idx => normals[idx]), bitmap,
                    zBuffer, lightDirection);
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

    public static void AddMinimapToBitmap(ImageInfo positions, Bgra32Bitmap bitmap)
    {
        const int mapHeight = 300;
        const int mapWidth = 300;
        const int borderDistance = 10;

        var mapX = bitmap.PixelWidth - mapWidth - borderDistance;

        var startX = mapX + mapWidth / 2;
        const int startY = borderDistance + mapHeight / 2;

        const int pixelsInHorizontalAxis = 4000;
        const int pixelsInVerticalAxis = 4000;
        
        const float horizontalProportion = (float)mapWidth / pixelsInHorizontalAxis;
        const float verticalProportion = (float)mapHeight / pixelsInVerticalAxis;

        var objectX = (int)Math.Round(startX + horizontalProportion * positions.PositionX);
        var objectY = (int)Math.Round(startY + verticalProportion * positions.PositionZ);

        var camPosition = VertexTransformer.ToOrthogonal(positions.CameraPosition);
        var cameraX = (int)Math.Round(startX + horizontalProportion * camPosition.X);
        var cameraY = (int)Math.Round(startY + verticalProportion * camPosition.Z);

        DrawLine(mapX, borderDistance, mapX + mapWidth, borderDistance, 0, 0, 0, bitmap); // top left to top right
        DrawLine(mapX + mapWidth, borderDistance, mapX + mapWidth, borderDistance + mapHeight, 0, 0, 0, bitmap); // top right to bottom right
        DrawLine(mapX + mapWidth, borderDistance + mapHeight, mapX, borderDistance + mapHeight, 0, 0, 0, bitmap); // bottom right to bottom left
        DrawLine(mapX, borderDistance + mapHeight, mapX, borderDistance, 0, 0, 0, bitmap); // bottom left to top left

        // cross
        DrawLine(mapX + mapWidth / 2.0f, borderDistance, mapX + mapWidth / 2.0f, borderDistance + mapHeight, 0, 0, 0, bitmap);
        DrawLine(mapX, borderDistance + mapHeight / 2.0f, mapX + mapWidth, borderDistance + mapHeight / 2.0f, 0, 0, 0, bitmap);

        DrawCircle(objectX, objectY, 5, 255, 0, 0, bitmap);
        DrawCircle(cameraX, cameraY, 5, 0, 255, 0, bitmap);
    }
}