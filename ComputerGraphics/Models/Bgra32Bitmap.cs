using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ComputerGraphics.Models
{
    public unsafe class Bgra32Bitmap
    {
        public Bgra32Bitmap(int pixelWidth, int pixelHeight)
        {
            Source = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgra32, null);
            _backBuffer = (byte*)Source.BackBuffer;
            _backBufferStride = Source.BackBufferStride;
            _bytesPerPixel = Source.Format.BitsPerPixel / 8;
            PixelWidth = Source.PixelWidth;
            PixelHeight = Source.PixelHeight;
        }

        private readonly byte* _backBuffer;
        private readonly int _backBufferStride;
        private readonly int _bytesPerPixel;

        public WriteableBitmap Source { get; }
        public int PixelWidth { get; }
        public int PixelHeight { get; }

        public void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            if (x <= 0 || x >= PixelWidth || y <= 0 || y >= PixelHeight)
                return;

            var address = _backBuffer + y * _backBufferStride + x * _bytesPerPixel;
            address[0] = b;
            address[1] = g;
            address[2] = r;
            address[3] = 255;
        }
    }
}