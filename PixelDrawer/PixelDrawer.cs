﻿using PEPlugin.SDX;
using System;
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
                pixels[i].FromARGB(array[p + 3], array[p + 2], array[p + 1], array[p]);
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
        public int Height => Length / Width;

        public Pixel this[int x, int y]
        {
            get => pixels[x * 4 + y * Width];
            set => pixels[x * 4 + y * Width] = value;
        }

        public PixelMap(Pixel[] pixels, int width)
        {
            this.pixels = pixels;
            Width = width;
        }

        public byte[] ToBytes()
        {
            byte[] vs = new byte[Length * 4];
            for (int i = 0; i < Length; i++)
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
        IntPtr ptr => map.Scan0;
        PixelMap pixels;
        bool IsLocking;

        Graphics graphics;

        /// <summary>
        /// 複製を返す
        /// </summary>
        public Bitmap GetCopy() => new Bitmap(canvas);

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
            Map();
            IsBottomUp = map.Stride < 0;

            graphics = Graphics.FromImage(canvas);
        }

        Color AverageColor(Color[] colors)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;
            byte a = 0;
            foreach (var c in colors)
            {
                r += c.R;
                g += c.G;
                b += c.B;
                a += c.A;
            }
            return Color.FromArgb(a / colors.Length, r / colors.Length, g / colors.Length, b / colors.Length);
        }

        void Map()
        {
            if (IsLocking)
                return;
            map = canvas.LockBits(new Rectangle(0, 0, canvas.Width, canvas.Height), ImageLockMode.ReadWrite, pixelFormat);
            IsLocking = true;
            IsBottomUp = map.Stride < 0;
            var pxls = new byte[Length];
            System.Runtime.InteropServices.Marshal.Copy(ptr, pxls, 0, Length);
            pixels = new PixelMap(Pixel.ArrayFrom(pxls), canvas.Width);
        }

        /// <summary>
        /// canvasのメモリロックを解除する
        /// </summary>
        public void Unmap()
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
                Map();

            System.Runtime.InteropServices.Marshal.Copy(pixels.ToBytes(), 0, ptr, pixels.Length);
        }

        int ToWidth(float value) => (value * Width).Round();
        int ToHeight(float value) => (value * Height).Round();

        /// <summary>
        /// 指定色の点を打つ
        /// Write要
        /// </summary>
        /// <param name="color">色</param>
        /// <param name="pos">位置（割合）</param>
        public void Plot(Color color, V2 pos)
        {
            pixels[ToWidth(pos.U), ToHeight(pos.V)].FromColor(color);
        }

        /// <summary>
        /// 指定色の点を打つ
        /// Write要
        /// </summary>
        /// <param name="colors">色</param>
        /// <param name="pos">位置（割合）</param>
        public void Plot(Color[] colors, V2[] pos)
        {
            for (int i = 0; i < pos.Length; i++)
            {
                Plot(colors[i], pos[i]);
            }
        }

        /// <summary>
        /// 入力ビットマップを拡縮して描画する
        /// Write不要
        /// </summary>
        public void fillImage(Bitmap bitmap, InterpolationMode mode = InterpolationMode.HighQualityBicubic)
        {
            graphics.InterpolationMode = mode;
            graphics.DrawImage(bitmap, 0, 0, Width, Height);
        }


        /// <summary>
        /// 多角形を描画する
        /// Write不要
        /// </summary>
        public void DrawPolygon(Color[] colors, V2[] points, int width)
        {
            var ps = points.Select(p => new Point(ToWidth(p.U), ToHeight(p.V))).ToArray();
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddPolygon(ps);
                using (PathGradientBrush brush = new PathGradientBrush(gp))
                {
                    brush.SurroundColors = colors;
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
            var ps = points.Select(p => new Point(ToWidth(p.U), ToHeight(p.V))).ToArray();
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddPolygon(ps);
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
                Unmap();
            canvas.Dispose();
            graphics.Dispose();
        }
    }
}