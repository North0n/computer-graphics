using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ComputerGraphics.models
{
    public unsafe class Bgra32Bitmap
    {
        public Bgra32Bitmap(int pixelWidth, int pixelHeight)
        {
            Source = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgra32, null);
            BackBuffer = (byte*)Source.BackBuffer;
            BackBufferStride = Source.BackBufferStride;
            BytesPerPixel = Source.Format.BitsPerPixel / 8;
            PixelWidth = Source.PixelWidth;
            PixelHeight = Source.PixelHeight;
        }

        private byte* BackBuffer { get; }
        private int BackBufferStride { get; }
        private int BytesPerPixel { get; }
        public WriteableBitmap Source { get; set; }
        public int PixelWidth { get; }
        public int PixelHeight { get; }

        public void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            if (x <= 0 || x >= PixelWidth || y <= 0 || y >= PixelHeight)
                return;

            var address = BackBuffer + y * BackBufferStride + x * BytesPerPixel;
            address[0] = b;
            address[1] = g;
            address[2] = r;
            address[3] = 255;
        }
    }
}