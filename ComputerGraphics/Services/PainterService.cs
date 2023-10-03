using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

    private static void DrawTriangle(List<Vector4> vertexes, List<Vector3> normals, Bgra32Bitmap bitmap,
        float[,] zBuffer, Vector3 lightDirection)
    {
        var intensity = normals.ConvertAll(n => Vector3.Dot(n, -lightDirection)).Average();
        // intensity = Math.Abs(intensity);
        if (intensity < 0)
            return;

        var (red, green, blue) = ((byte)(intensity * 200), (byte)(intensity * 200), (byte)(intensity * 200));

        var ordered = vertexes.OrderBy(v => v.Y).ToList();
        var up = ordered[2];
        var mid = ordered[1];
        var down = ordered[0];

        if (down.Y < 0)
            return;

        var upY = (int)up.Y;
        var midY = (int)mid.Y;
        var downY = (int)down.Y;

        up.Z = 1 / up.Z;
        down.Z = 1 / down.Z;
        mid.Z = 1 / mid.Z;

        var firstSegmentHeight = midY - downY;
        var secondSegmentHeight = upY - midY;

        var totalHeight = upY - downY;
        for (var i = 0; i < totalHeight; i++)
        {
            var y = i + downY;

            var secondHalf = i > firstSegmentHeight || midY == downY;
            var segmentHeight = secondHalf ? secondSegmentHeight : firstSegmentHeight;
            var alpha = (float)i / totalHeight;
            var beta = (float)(i - (secondHalf ? firstSegmentHeight : 0)) / segmentHeight;

            var a = down + (up - down) * alpha;
            var b = secondHalf ? mid + (up - mid) * beta : down + (mid - down) * beta;

            if (a.X > b.X) (a, b) = (b, a);

            var deltaX = b.X - a.X + 1;

            for (var x = (int)a.X; x <= (int)b.X; x++)
            {
                if (x >= bitmap.PixelWidth)
                    break;

                var p = (x - a.X) / deltaX;

                var z = 1 / (a.Z + p * (b.Z - a.Z));
                if (zBuffer[x, y] < z)
                {
                    zBuffer[x, y] = z;
                    bitmap.SetPixel(x, y, red, green, blue);
                }
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
                DrawTriangle(face.ConvertAll(idx => vertexes[idx]), curNormals.ConvertAll(idx => normals[idx]), bitmap,
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

        DrawCircle(objectX, objectY, 5, 255, 0, 0, bitmap);
        DrawCircle(cameraX, cameraY, 5, 0, 255, 0, bitmap);
    }
}