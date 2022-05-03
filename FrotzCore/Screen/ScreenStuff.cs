namespace Frotz.Screen
{

    using System;
    using System.Text;

    public sealed class ZKeyPressEventArgs : EventArgs
    {
        public char KeyPressed { get; private set; }

        public ZKeyPressEventArgs(char KeyPressed)
        {
            this.KeyPressed = KeyPressed;
        }
    }

    public readonly struct ZPoint
    {
        public readonly int X;
        public readonly int Y;

        public ZPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator ZPoint(ValueTuple<int, int> pair) => new(pair.Item1, pair.Item2);
    }

    public readonly struct ZSize
    {
        public readonly int Height;
        public readonly int Width;

        public ZSize(int height, int width)
        {
            Height = height;
            Width = width;
        }
        public ZSize(double height, double width) : this(Convert.ToInt32(height), Convert.ToInt32(width)) { }

        public static implicit operator ZSize(ValueTuple<int, int> pair)
            => new(pair.Item1, pair.Item2);

        public static implicit operator ZSize(ValueTuple<double, double> pair)
            => new(pair.Item1, pair.Item2);

        public static readonly ZSize Empty = new(0, 0);
    }

    public readonly struct ScreenMetrics
    {
        public readonly ZSize FontSize;
        public readonly ZSize WindowSize;
        public readonly int Rows;
        public readonly int Columns;
        public readonly int Scale;

        public ScreenMetrics(ZSize fontSize, ZSize windowSize, int rows, int columns, int scale)
        {
            FontSize = fontSize;
            WindowSize = windowSize;
            Rows = rows;
            Columns = columns;
            Scale = scale;
        }
        public (int Rows, int Columnns) Dimensions => (Rows, Columns);
    }

    public class FontChanges
    {
        public int Offset { get; set; }
        public int StartCol { get; private set; }
        public int Count => _sb.Length;
        public int Style => FontAndStyle.Style;
        public int Font => FontAndStyle.Font;
        public string Text => _sb.ToString();
        public int Line { get; set; }

        public CharDisplayInfo FontAndStyle { get; set; }

        private readonly StringBuilder _sb;

        internal void AddChar(char c) => _sb.Append(c);

        public FontChanges(int startCol, int count, CharDisplayInfo FandS)
        {
            StartCol = startCol;
            FontAndStyle = FandS;
            _sb = new(count);
        }
    }
}