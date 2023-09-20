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

    private static void DrawPolygon(IReadOnlyList<Vector4> vertexes, Bgra32Bitmap bitmap)
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

                if (Math.Abs(nextVertex.Y - vertex.Y) < 0.01)
                {
                    var v1 = new IntVector2D(Round(vertex.X), Round(vertex.Y));
                    intersections.Enqueue(v1, v1);
                    var v2 = new IntVector2D(Round(nextVertex.X), Round(nextVertex.Y));
                    intersections.Enqueue(v2, v2);
                    continue;
                }


                var k = (nextVertex.Y - vertex.Y) / (nextVertex.X - vertex.X);
                float x;
                if (float.IsFinite(k))
                {
                    var yb = nextVertex.Y - k * nextVertex.X;
                    x = (y - yb) / k;
                }
                else
                {
                    if (y < nextVertex.Y ^ y > vertex.Y)
                        x = float.NaN;
                    else
                        x = vertex.X; // Если вершины по разные стороны от y, тогда есть пересечение
                }
                // var x = (y - vertex.Y) / (nextVertex.Y - vertex.Y) * (nextVertex.X - vertex.X) + vertex.X;
                // Don't add intersection point if it is not inside polygon
                if (float.IsNaN(x) || x < Math.Min(vertex.X, nextVertex.X) || x > Math.Max(vertex.X, nextVertex.X))
                    continue;

                var vec2 = new IntVector2D(Round(x), y);
                intersections.Enqueue(vec2, vec2);
            }
        }

        var (r, g, b) = ((byte)Random.Shared.Next(255), (byte)Random.Shared.Next(255), (byte)Random.Shared.Next(255));
        while (intersections.Count >= 2)
        {
            var vec1 = intersections.Dequeue();
            var vec2 = intersections.Dequeue();

            for (var x = vec1.X; x <= vec2.X; ++x)
            {
                bitmap.SetPixel(x, vec1.Y, r, g, b);
            }
        }
    }

    public static Bgra32Bitmap DrawModel(Vector4[] vertexes, List<List<int>> faces, int width, int height)
    {
        Bgra32Bitmap bitmap = new(width, height);
        bitmap.Source.Lock();

        Parallel.ForEach(Partitioner.Create(0, faces.Count), range =>
        {
            for (var j = range.Item1; j < range.Item2; ++j)
            {
                var face = faces[j];
                DrawPolygon(face.ConvertAll(idx => vertexes[idx]), bitmap);
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

        var cameraX = (int)Math.Round(startX + horizontalProportion * positions.CameraPosition.X);
        var cameraY = (int)Math.Round(startY + verticalProportion * positions.CameraPosition.Z);

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