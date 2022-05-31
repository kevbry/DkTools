using System;
using System.IO;

namespace DKX.Compilation
{
    public struct Span
    {
        public static readonly Span Empty = default;

        private string _pathName;
        private int _start;
        private int _end;

        public Span(string pathName, int start, int end)
        {
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));

            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (end < start) throw new ArgumentOutOfRangeException(nameof(end));
            _start = start;
            _end = end;
        }

        public string PathName => _pathName;
        public int Start => _start;
        public int End => _end;
        public bool IsEmpty => _pathName == null;
        public int Length => _end - _start;

        public static Span operator +(Span a, Span b)
        {
            if (a.PathName != b.PathName) throw new ArgumentException("Both spans are not from the same file.");
            return new Span(a.PathName, a.Start < b.Start ? a.Start : b.Start, a.End > b.End ? a.End : b.End);
        }

        public static Span operator +(Span a, int b)
        {
            return new Span(a.PathName, a.Start < b ? a.Start : b, a.End > b ? a.End : b);
        }

        public Span Envelope(Span x) => this + x;
    }
}
