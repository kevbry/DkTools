using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DKX.Compilation.Tokens
{
    public class DkxTokenCollection : IEnumerable<DkxToken>, IEnumerable
    {
        private List<DkxToken> _tokens = new List<DkxToken>();

        public IEnumerator<DkxToken> GetEnumerator() => new DkxTokenEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new DkxTokenEnumerator(this);

        public int Count => _tokens.Count;

        public override string ToString() => string.Join(" ", _tokens);

        public DkxToken this[int index] => index >= 0 && index < _tokens.Count ? _tokens[index] : default;

        public void Add(DkxToken token) => _tokens.Add(token);

        public void AddRange(IEnumerable<DkxToken> tokens) => _tokens.AddRange(tokens);

        public int FindIndex(Func<DkxToken, bool> callback, int startIndex = 0)
        {
            var index = startIndex;
            while (index < _tokens.Count)
            {
                if (callback(_tokens[index])) return index;
                index++;
            }
            return -1;
        }

        public int FindIndex(Func<DkxToken, int, bool> callback, int startIndex = 0)
        {
            var index = startIndex;
            while (index < _tokens.Count)
            {
                if (callback(_tokens[index], index)) return index;
                index++;
            }
            return -1;
        }

        public IEnumerable<int> FindIndices(Func<DkxToken,bool> callback, int startIndex = 0)
        {
            var index = startIndex;
            while (index < _tokens.Count)
            {
                if (callback(_tokens[index])) yield return index;
                index++;
            }
        }

        public IEnumerable<int> FindIndices(Func<DkxToken,int,bool> callback, int startIndex = 0)
        {
            var index = startIndex;
            while (index < _tokens.Count)
            {
                if (callback(_tokens[index], index)) yield return index;
                index++;
            }
        }

        public DkxTokenCollection GetRange(int start, int count = -1)
        {
            var ret = new DkxTokenCollection();
            var end = count < 0 ? _tokens.Count : start + count;
            for (var i = start; i < end; i++)
            {
                if (i >= 0 && i < _tokens.Count) ret.Add(_tokens[i]);
            }
            return ret;
        }

        public int FindStatementEnd(int startPosition)
        {
            var pos = startPosition;

            while (pos < _tokens.Count)
            {
                switch (_tokens[pos].Type)
                {
                    case DkxTokenType.StatementEnd:
                        return pos;
                    case DkxTokenType.Invalid:
                        switch (_tokens[pos].Char)
                        {
                            case ')':
                            case '}':
                            case ']':
                                return -1;
                        }
                        break;
                }
                pos++;
            }

            return -1;
        }

        public IEnumerable<DkxToken> GetUnused(TokenUseTracker used)
        {
            foreach (var token in _tokens)
            {
                if (!used.IsUsed(token)) yield return token;
            }
        }

        public IEnumerable<DkxTokenCollection> SplitByType(DkxTokenType type)
        {
            var current = new DkxTokenCollection();

            foreach (var token in _tokens)
            {
                if (token.Type == type)
                {
                    yield return current;
                    current = new DkxTokenCollection();
                }
                else
                {
                    current.Add(token);
                }
            }

            yield return current;
        }

        public Span Span
        {
            get
            {
                if (_tokens.Count == 0) return Span.Empty;

                var span = _tokens[0].Span;
                for (int i = 1, ii = _tokens.Count; i < ii; i++) span = span.Envelope(_tokens[i].Span);
                return span;
            }
        }

        public DkxTokenStream ToStream() => new DkxTokenStream(this);

        public class DkxTokenEnumerator : IEnumerator<DkxToken>, IEnumerator
        {
            private DkxTokenCollection _collection;
            private int _index;

            public DkxTokenEnumerator(DkxTokenCollection collection)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
                _index = -1;
            }

            public void Dispose() { }

            public DkxToken Current => _collection._tokens[_index];

            object IEnumerator.Current => _collection._tokens[_index];

            public bool MoveNext()
            {
                _index++;
                return _index < _collection._tokens.Count;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}
