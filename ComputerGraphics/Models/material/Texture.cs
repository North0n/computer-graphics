using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pfim;

namespace ComputerGraphics.Models.material;

public class Texture
{
    private readonly Vector3[,] _colors;
    private readonly int _bufferWidth;
    private readonly int _bufferHeight;

    public Texture(string path)
    {
        var bitmap = GetBitmapFromFile(path);
        _bufferWidth = bitmap.PixelWidth;
        _bufferHeight = bitmap.PixelHeight;
        _colors = new Vector3[_bufferWidth, _bufferHeight];

        var stride = bitmap.PixelWidth * (bitmap.Format.BitsPerPixel / 8);
        var pixels = new byte[bitmap.PixelHeight * stride];

        bitmap.CopyPixels(pixels, stride, 0);
        for (var y = 0; y < _bufferHeight; y++)
        {
            for (var x = 0; x < _bufferWidth; x++)
            {
                var startIndex = stride * (_bufferHeight - 1 - y) + x * bitmap.Format.BitsPerPixel / 8;
                var b = pixels[startIndex + 0] / 255f;
                var g = pixels[startIndex + 1] / 255f;
                var r = pixels[startIndex + 2] / 255f;
                _colors[x, y] = new Vector3(r, g, b);
            }
        }
    }

    public Vector3 GetPixel(float x, float y)
    {
        x -= MathF.Floor(x);
        y -= MathF.Floor(x);
        return _colors[(int)(_bufferWidth * x), (int)(_bufferHeight * y)];
    }

    private static PixelFormat PixelFormat(IImage image) =>
        image.Format switch
        {
            ImageFormat.Rgb24 => PixelFormats.Bgr24,
            ImageFormat.Rgba32 => PixelFormats.Bgra32,
            ImageFormat.Rgb8 => PixelFormats.Gray8,
            ImageFormat.R5g5b5a1 => PixelFormats.Bgr555,
            ImageFormat.R5g5b5 => PixelFormats.Bgr555,
            ImageFormat.R5g6b5 => PixelFormats.Bgr565,
            _ => throw new Exception($"Unable to convert {image.Format} to WPF PixelFormat")
        };

    private static BitmapSource GetBitmapFromFile(string path)
    {
        var fileExt = Path.GetExtension(path).Trim();
        BitmapSource bitmap;

        if (fileExt == ".tga")
        {
            using var image = Pfimage.FromFile(path);
            var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            var address = handle.AddrOfPinnedObject();
            bitmap = BitmapSource.Create(image.Width, image.Height, 96.0, 96.0, PixelFormat(image), null, address,
                image.DataLen, image.Stride);
            handle.Free();
        }
        else
        {
            bitmap = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
        }

        BitmapPalette palette = new(new List<Color> { Colors.Blue, Colors.Green, Colors.Blue });
        return new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, palette, 0.0);
    }
}