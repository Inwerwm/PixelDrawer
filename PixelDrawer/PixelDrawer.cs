using PEPlugin.SDX;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace PixelDrawer
{
    public struct Pixel
    {
        byte[] elements;

        /// <summary>
        /// 青
        /// </summary>
        public byte B { get => elements[0]; set => elements[0] = value; }

        /// <summary>
        /// 緑
        /// </summary>
        public byte G { get => elements[1]; set => elements[1] = value; }

        /// <summary>
        /// 赤
        /// </summary>
        public byte R { get => elements[2]; set => elements[2] = value; }

        /// <summary>
        /// 非透明度
        /// </summary>
        public byte A { get => elements[3]; set => elements[3] = value; }

        public Color Color
        {
            get => Color.FromArgb(A, R, G, B);
            set
            {
                A = value.A;
                R = value.R;
                G = value.G;
                B = value.B;
            }
        }

        public Pixel(byte r = 0x00, byte g = 0x00, byte b = 0x00, byte a = 0xFF)
        {
            elements = new byte[4] { b, g, r, a };
        }

        public Pixel(byte[] vs)
        {
            if (vs.Length == 4)
                elements = vs;
            else if (vs.Length > 4)
                elements = vs.Take(4).ToArray();
            else
            {
                elements = new byte[4];
                for (int i = 0; i < vs.Length; i++)
                {
                    elements[i] = vs[i];
                }
            }
        }

        public void FromARGB(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public void FromRGB(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public void FromColor(Color color)
        {
            Color = color;
        }

        public byte[] ToBytes() => elements;

        public static Pixel[] ArrayFrom(byte[] array)
        {
            Pixel[] pixels = new Pixel[array.Length / 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = i * 4;
                pixels[i] = new Pixel(array[p + 2], array[p + 1], array[p + 0], array[p + 3]);
            }
            return pixels;
        }
    }

    public struct PixelMap
    {
        Pixel[] pixels;

        /// <summary>
        /// バイト長（ピクセル数×4）
        /// </summary>
        public int Length => pixels.Length * 4;
        /// <summary>
        /// ピクセル数
        /// </summary>
        public int Count => pixels.Length;
        public int Width { get; private set; }
        public int Height => Count / Width;

        public Pixel this[int x, int y]
        {
            get => pixels[x + y * Width];
            set => pixels[x + y * Width] = value;
        }

        public PixelMap(Pixel[] pixels, int width)
        {
            this.pixels = pixels;
            Width = width;
        }

        public byte[] ToBytes()
        {
            byte[] vs = new byte[Length];
            for (int i = 0; i < Count; i++)
            {
                int p = i * 4;
                vs[p + 0] = pixels[i].B;
                vs[p + 1] = pixels[i].G;
                vs[p + 2] = pixels[i].R;
                vs[p + 3] = pixels[i].A;
            }
            return vs;
        }
    }

    /// <summary>
    /// テクスチャ書込クラス
    /// </summary>
    public class PixelDrawer
    {
        readonly PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
        readonly int pixelSize = 4;

        Bitmap canvas;
        BitmapData map;
        PixelMap pixels;
        bool IsLocking;

        public Bitmap Canvas
        {
            get
            {
                if (IsLocking)
                    UnLock();
                return canvas;
            }
        }

        /// <summary>
        /// ボトムアップ形式の真偽値
        /// </summary>
        public bool IsBottomUp { get; private set; }
        /// <summary>
        /// 画像の総バイト数
        /// </summary>
        public int Length { get => pixelSize * canvas.Width * canvas.Height; }
        /// <summary>
        /// 画像幅（横ピクセル数 - 1）
        /// </summary>
        public int Width { get; private set; }
        /// <summary>
        /// 画像丈（縦ピクセル数 - 1）
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">幅（ピクセル数は+1になる）</param>
        /// <param name="height">丈（ピクセル数は+1になる）</param>
        public PixelDrawer(int width, int height)
        {
            IsLocking = false;
            Width = width;
            Height = height;
            canvas = new Bitmap(Width + 1, Height + 1);
        }

        Color AverageColor(Color[] colors)
        {
            int r = 0;
            int g = 0;
            int b = 0;
            int a = 0;
            foreach (var c in colors)
            {
                r += c.R;
                g += c.G;
                b += c.B;
                a += c.A;
            }
            return Color.FromArgb(a / colors.Length, r / colors.Length, g / colors.Length, b / colors.Length);
        }

        /// <summary>
        /// canvasをメモリロックする
        /// </summary>
        public void Lock()
        {
            if (IsLocking)
                return;
            map = canvas.LockBits(new Rectangle(0, 0, canvas.Width, canvas.Height), ImageLockMode.ReadWrite, pixelFormat);
            IsLocking = true;
            IsBottomUp = map.Stride < 0;
            var pxls = new byte[Length];
            System.Runtime.InteropServices.Marshal.Copy(map.Scan0, pxls, 0, Length);
            pixels = new PixelMap(Pixel.ArrayFrom(pxls), canvas.Width);
        }

        /// <summary>
        /// canvasのメモリロックを解除する
        /// </summary>
        public void UnLock()
        {
            if (!IsLocking)
                return;
            canvas.UnlockBits(map);
            IsLocking = false;
        }

        /// <summary>
        /// 描画処理をキャンパスに反映する
        /// </summary>
        public void Write()
        {
            if (!IsLocking)
                return;
            System.Runtime.InteropServices.Marshal.Copy(pixels.ToBytes(), 0, map.Scan0, pixels.Length);
            UnLock();
        }

        int ToWidth(float value) => (value * Width).Round();
        int ToHeight(float value) => (value * Height).Round();

        public void PlotSquare(Color color, int x, int y, int width, int height)
        {
            PlotSquare(color, new Rectangle(x, y, width, height));
        }

        public void PlotSquare(Color color, Rectangle rectangle)
        {
            Lock();

            for (int i = rectangle.Left; i < rectangle.Right; i++)
            {
                for (int j = rectangle.Top; j < rectangle.Bottom; j++)
                {
                    if (i.IsInside(-1, pixels.Width) && j.IsInside(-1, pixels.Height))
                        pixels[i, j].FromColor(color);
                }
            }
        }

        /// <summary>
        /// 指定色の点を打つ
        /// Write要
        /// </summary>
        /// <param name="color">色</param>
        /// <param name="pos">位置（割合）</param>
        public void Plot(Color color, V2 pos, int size = 1)
        {
            Lock();

            int u = ToWidth(pos.U);
            int v = ToHeight(pos.V);
            if (size > 1)
            {
                PlotSquare(color, u - size + 1, v - size + 1, 2 * size - 1, 2 * size - 1);
            }
            else
            {
                if (u.IsInside(-1, pixels.Width) && v.IsInside(-1, pixels.Height))
                    return;
                pixels[u, v].FromColor(color);
            }
        }

        /// <summary>
        /// 指定色の点を打つ
        /// Write要
        /// </summary>
        /// <param name="colors">色</param>
        /// <param name="pos">位置（割合）</param>
        public void Plot(Color[] colors, V2[] pos, int size = 1)
        {
            for (int i = 0; i < pos.Length; i++)
            {
                Plot(colors[i], pos[i], size);
            }
        }

        /// <summary>
        /// 入力ビットマップを拡縮して描画する
        /// Write不要
        /// </summary>
        public void fillImage(Bitmap bitmap, InterpolationMode mode = InterpolationMode.HighQualityBicubic)
        {
            UnLock();
            using (Graphics graphics = Graphics.FromImage(canvas))
            {
                graphics.InterpolationMode = mode;
                graphics.DrawImage(bitmap, 0, 0, Width, Height);
            }
        }


        /// <summary>
        /// 多角形を描画する
        /// Write不要
        /// </summary>
        public void DrawPolygon(Color[] colors, V2[] points, float width)
        {
            UnLock();
            var ps = points.Select(p => new Point(ToWidth(p.U), ToHeight(p.V))).ToArray();
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddPolygon(ps);
                using (PathGradientBrush brush = new PathGradientBrush(gp))
                {
                    brush.SurroundColors = colors;
                    using (Graphics graphics = Graphics.FromImage(canvas))
                    using (Pen pen = new Pen(brush))
                    {
                        pen.Width = width;
                        graphics.DrawPolygon(pen, ps);
                    }
                }
            }
        }

        /// <summary>
        /// 多角形に塗りつぶす
        /// Write不要
        /// </summary>
        public void FillPolygon(Color[] colors, V2[] points)
        {
            UnLock();
            var ps = points.Select(p => new Point(ToWidth(p.U), ToHeight(p.V))).ToArray();
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddPolygon(ps);
                using (Graphics graphics = Graphics.FromImage(canvas))
                using (PathGradientBrush brush = new PathGradientBrush(gp))
                {
                    brush.SurroundColors = colors;
                    brush.CenterColor = AverageColor(colors);
                    graphics.FillPolygon(brush, ps);
                }
            }
        }

        ~PixelDrawer()
        {
            if (IsLocking)
                UnLock();
            canvas.Dispose();
        }
    }
}
