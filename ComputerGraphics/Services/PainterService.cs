using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using ComputerGraphics.models;

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
}