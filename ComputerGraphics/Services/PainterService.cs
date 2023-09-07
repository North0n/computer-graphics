using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using ComputerGraphics.Models;

namespace ComputerGraphics.Services;

public static class PainterService
{
    private static void DrawLine(float x0, float y0, float x1, float y1, byte r, byte g, byte b, Bgra32Bitmap bitmap)
    {
        // ReSharper disable InconsistentNaming
        var x0i = (int)Math.Round(x0, MidpointRounding.AwayFromZero);
        var y0i = (int)Math.Round(y0, MidpointRounding.AwayFromZero);
        var x1i = (int)Math.Round(x1, MidpointRounding.AwayFromZero);
        var y1i = (int)Math.Round(y1, MidpointRounding.AwayFromZero);
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
            var xi = (int)Math.Round(x, MidpointRounding.AwayFromZero);
            var yi = (int)Math.Round(y, MidpointRounding.AwayFromZero);
            bitmap.SetPixel(xi, yi, r, g, b);
            x += dx;
            y += dy;
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
                for (var i = 0; i < face.Count; i++)
                {
                    var a = vertexes[face[i]];
                    var b = vertexes[face[(i + 1) % face.Count]];
                    DrawLine(a.X, a.Y, b.X, b.Y, 0, 0, 0, bitmap);
                }
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
        var mapHeight = 300;
        var mapWidth = 300;
        var borderDistance = 10;

        var mapX = bitmap.PixelWidth - mapWidth - borderDistance;
        var mapY = borderDistance;

        var startX = mapX + mapWidth / 2;
        var startY = mapY + mapHeight / 2;

        var pixelsInHorizontalAxis = 4000;
        var pixelsInVerticalAxis = 4000;
        
        var horizontalProportion = (float)mapWidth / pixelsInHorizontalAxis;
        var verticalProportion = (float)mapHeight / pixelsInVerticalAxis;

        var objectX = (int)Math.Round(startX + (float)horizontalProportion * positions.PositionX);
        var objectY = (int)Math.Round(startY + (float)verticalProportion * positions.PositionZ);

        var cameraX = (int)Math.Round(startX + (float)horizontalProportion * positions.CameraPosition.X);
        var cameraY = (int)Math.Round(startY + (float)verticalProportion * positions.CameraPosition.Z);

        DrawLine(mapX, mapY, mapX + mapWidth, mapY, 0, 0, 0, bitmap); // top left to top right
        DrawLine(mapX + mapWidth, mapY, mapX + mapWidth, mapY + mapHeight, 0, 0, 0, bitmap); // top right to bottom right
        DrawLine(mapX + mapWidth, mapY + mapHeight, mapX, mapY + mapHeight, 0, 0, 0, bitmap); // bottom right to bottom left
        DrawLine(mapX, mapY + mapHeight, mapX, mapY, 0, 0, 0, bitmap); // bottom left to top left

        // cross
        DrawLine(mapX + mapWidth / 2, mapY, mapX + mapWidth / 2, mapY + mapHeight, 0, 0, 0, bitmap);
        DrawLine(mapX, mapY + mapHeight / 2, mapX + mapWidth, mapY + mapHeight / 2, 0, 0, 0, bitmap);

        DrawCircle(objectX, objectY, 5, 255, 0, 0, bitmap);
        DrawCircle(cameraX, cameraY, 5, 0, 255, 0, bitmap);
    }
}