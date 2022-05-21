using System;
using System.Collections;
using System.Collections.Generic;

namespace DKX.Compilation.Tokens
{
    public class DkxTokenCollection : IEnumerable<DkxToken>, IEnumerable
    {
        private List<DkxToken> _tokens = new List<DkxToken>();

        public IEnumerator<DkxToken> GetEnumerator() => new DkxTokenEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new DkxTokenEnumerator(this);

        public int Count => _tokens.Count;

        public DkxToken this[int index] => index >= 0 && index < _tokens.Count ? _tokens[index] : default;

        public void Add(DkxToken token) => _tokens.Add(token);

        public void AddRange(IEnumerable<DkxToken> tokens) => _tokens.AddRange(tokens);

        public IEnumerable<int> FindIndices(Func<DkxToken,bool> callback)
        {
            var index = 0;
            foreach (var token in _tokens)
            {
                if (callback(token)) yield return index;
                index++;
            }
        }

        public IEnumerable<int> FindIndices(Func<DkxToken,int,bool> callback)
        {
            var index = 0;
            foreach (var token in _tokens)
            {
                if (callback(token, index)) yield return index;
                index++;
            }
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
