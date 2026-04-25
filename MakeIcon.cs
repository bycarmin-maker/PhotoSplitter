using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IconMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2) return;
            string pngPath = args[0];
            string icoPath = args[1];

            using (Image img = Image.FromFile(pngPath))
            using (Bitmap b = new Bitmap(256, 256))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.Transparent);

                    // iOS squircle: radius = 약 22.5% of size (Apple 공식 비율)
                    int size = 256;
                    int r = 58;
                    GraphicsPath path = new GraphicsPath();
                    path.AddArc(0,        0,        r, r, 180, 90);
                    path.AddArc(size - r, 0,        r, r, 270, 90);
                    path.AddArc(size - r, size - r, r, r, 0,   90);
                    path.AddArc(0,        size - r, r, r, 90,  90);
                    path.CloseFigure();

                    g.SetClip(path);
                    // 이미지가 이미 정사각형이면 그대로 꽉 채워 그리기
                    g.DrawImage(img, new Rectangle(0, 0, size, size));
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    b.Save(ms, ImageFormat.Png);
                    byte[] pngBytes = ms.ToArray();

                    using (FileStream fs = new FileStream(icoPath, FileMode.Create))
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        bw.Write((short)0);  // Reserved
                        bw.Write((short)1);  // ICO type
                        bw.Write((short)1);  // 1 image
                        bw.Write((byte)0);   // Width  (0 = 256)
                        bw.Write((byte)0);   // Height (0 = 256)
                        bw.Write((byte)0);   // Color count
                        bw.Write((byte)0);   // Reserved
                        bw.Write((short)1);  // Color planes
                        bw.Write((short)32); // Bits per pixel
                        bw.Write(pngBytes.Length);
                        bw.Write(22);        // Offset to image data
                        bw.Write(pngBytes);
                    }
                }
            }
        }
    }
}
