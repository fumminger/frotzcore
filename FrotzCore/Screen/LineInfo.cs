using System;
using System.Collections.Generic;

namespace Frotz.Screen
{

    public class LineInfo : IDisposable
    {
        private readonly char[] _chars;
        private readonly CharDisplayInfo[] _styles;
        private readonly object _lockObj = new();
        private PooledList<FontChanges>? _changes;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; }
        public int LastCharSet { get; private set; }

        public LineInfo(int lineWidth)
        {
            _chars = new char[lineWidth];
            _styles = new CharDisplayInfo[lineWidth];

            Array.Fill(_chars, ' ');
            Array.Fill(_styles, default);

            Width = lineWidth;

            LastCharSet = -1;
        }

        public void SetChar(int pos, char c, CharDisplayInfo FandS = default)
        {
            if ((uint)pos >= (uint)Width)
                throw new ArgumentOutOfRangeException(nameof(pos));

            lock (_lockObj)
            {
                _chars[pos] = c;
                _styles[pos] = FandS;
                LastCharSet = Math.Max(pos, LastCharSet);

                _changes?.Dispose();
                _changes = null;
            }
        }

        public void SetChars(int pos, ReadOnlySpan<char> chars, CharDisplayInfo FandS = default)
        {
            if ((uint)pos >= (uint)Width)
                throw new ArgumentOutOfRangeException(nameof(pos));

            if ((uint)pos + chars.Length >= (uint)Width)
                throw new ArgumentOutOfRangeException(nameof(chars), "Too many chars to fit in line.");

            lock (_lockObj)
            {
                chars.CopyTo(_chars.AsSpan().Slice(pos));
                _styles.AsSpan().Slice(pos).Fill(FandS);
                LastCharSet = Math.Max(pos + chars.Length, Width);

                if (_changes is not null)
                {
                    _changes.Dispose();
                    _changes = null;
                }
            }
        }

        public void AddChar(char c, CharDisplayInfo FandS) => SetChar(++LastCharSet, c, FandS);

        public void ClearLine()
        {
            ClearChars(0, Width);
            LastCharSet = -1;
        }

        public void ClearChars(int left, int right)
        {
            if ((uint)left >= (uint)Width)
                throw new ArgumentOutOfRangeException(nameof(left));

            if ((uint)left + right >= (uint)Width)
                throw new ArgumentOutOfRangeException(nameof(right), "Too many chars to fit in line.");

            lock (_lockObj)
            {
                _chars.AsSpan().Slice(left, right - left).Fill(' ');
                _styles.AsSpan().Slice(left, right - left).Fill(default);
                LastCharSet = Math.Max(left + right, Width);

                if (_changes is not null)
                {
                    _changes.Dispose();
                    _changes = null;
                }
            }
        }

        public ReadOnlySpan<char> CurrentChars => _chars.AsSpan().Slice(0, LastCharSet + 1);

        public void Replace(int start, ReadOnlySpan<char> newString) => SetChars(start, newString);

        public IReadOnlyList<FontChanges> GetTextWithFontInfo()
        {
            if (_changes == null)
            {
                lock (_lockObj)
                {
                    if (_changes == null)
                    {
                        _changes = new PooledList<FontChanges>(Width);
                        var chars = CurrentChars;

                        var fc = new FontChanges(-1, 0, new CharDisplayInfo(-1, 0, 0, 0));
                        var styles = _styles;
                        for (int i = 0; i < Width; i++)
                        {
                            if (!styles[i].Equals(fc.FontAndStyle))
                            {
                                fc = new FontChanges(i, Width, styles[i]);
                                fc.AddChar(chars[i]);
                                _changes.Add(fc);
                            }
                            else
                            {
                                fc.AddChar(chars[i]);
                            }
                        }
                    }
                }
            }

            return _changes;
        }

        public ReadOnlySpan<char> GetChars() => _chars.AsSpan().Slice(0, Width);

        public ReadOnlySpan<char> GetChars(int start, int length) => _chars.AsSpan().Slice(start, length);

        public override string ToString() => GetChars().ToString();

        public CharDisplayInfo GetFontAndStyle(int column) => _styles[column];

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            //_styles.Dispose();
            //_chars.Dispose();
            //_changes?.Dispose();
        }
    }
}