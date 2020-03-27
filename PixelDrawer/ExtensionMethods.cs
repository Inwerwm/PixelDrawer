using PEPlugin.Pmx;
using PEPlugin.SDX;
using System;
using System.Drawing;

namespace PixelDrawer
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// 指定された閉区間の範囲内であるかを判断します。
        /// </summary>
        /// <param name="i"></param>
        /// <param name="lower">下限（自身も含む）</param>
        /// <param name="upper">上限（自身も含む）</param>
        /// <returns></returns>
        public static bool IsWithin<T>(this T i, T lower, T upper) where T : IComparable
        {
            if (upper.CompareTo(lower) < 0)
                throw new ArgumentOutOfRangeException("IsWithin<T>:下限値が上限値よりも大きいです。");
            return i.CompareTo(lower) * upper.CompareTo(i) >= 0;
        }

        /// <summary>
        /// 指定された開区間の範囲内であるかを判断します。
        /// </summary>
        /// <param name="i"></param>
        /// <param name="lower">下限（自身は含まない）</param>
        /// <param name="upper">上限（自身は含まない）</param>
        /// <returns></returns>
        public static bool IsInside<T>(this T i, T lower, T upper) where T : IComparable
        {
            if (upper.CompareTo(lower) < 0)
                throw new ArgumentOutOfRangeException("IsInside<T>:下限値が上限値よりも大きいです。");
            return i.CompareTo(lower) * upper.CompareTo(i) > 0;
        }

        /// <summary>
        /// 末尾に改行文字を加えてstringに変換する
        /// </summary>
        public static string ToStringL(this object value)
        {
            return value.ToString() + Environment.NewLine;
        }

        /// <summary>
        /// 「名前 = 値↲」の形で出力する
        /// </summary>
        /// <param name="name">名前</param>
        public static string ToStringN(this object value, string name)
        {
            return name + " = " + value.ToStringL();
        }

        public static PointF ToPointF(this V2 vertex)
        {
            return new PointF(vertex.X, vertex.Y);
        }

        public static PointF ToPointF(this V2 vertex, int Width, int Height)
        {
            return new PointF(vertex.X * Width, vertex.Y * Height);
        }

        public static PointF[] ToPointF(this IPXFace face)
        {
            return new PointF[3] { face.Vertex1.UV.ToPointF(), face.Vertex2.UV.ToPointF(), face.Vertex3.UV.ToPointF() };
        }

        public static PointF[] ToPointF(this IPXFace face, int Width, int Height)
        {
            return new PointF[3] { face.Vertex1.UV.ToPointF(Width, Height), face.Vertex2.UV.ToPointF(Width, Height), face.Vertex3.UV.ToPointF(Width, Height) };
        }

        public static Point ToPoint(this V2 vertex)
        {
            return new Point((int)Math.Round(vertex.X, MidpointRounding.AwayFromZero), (int)Math.Round(vertex.Y, MidpointRounding.AwayFromZero));
        }

        public static Point ToPoint(this V2 vertex, int Width, int Height)
        {
            return new Point((int)Math.Round(vertex.X * Width, MidpointRounding.AwayFromZero), (int)Math.Round(vertex.Y * Height, MidpointRounding.AwayFromZero));
        }

        public static Point[] ToPoint(this IPXFace face)
        {
            return new Point[3] { face.Vertex1.UV.ToPoint(), face.Vertex2.UV.ToPoint(), face.Vertex3.UV.ToPoint() };
        }

        public static Point[] ToPoint(this IPXFace face, int Width, int Height)
        {
            return new Point[3] { face.Vertex1.UV.ToPoint(Width, Height), face.Vertex2.UV.ToPoint(Width, Height), face.Vertex3.UV.ToPoint(Width, Height) };
        }
        public static int Round(this float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

        public static string Print(this Point point) => "(" + point.X + ", " + point.Y + ")";
        public static string Print(this PointF point) => "(" + point.X + ", " + point.Y + ")";
    }
}
