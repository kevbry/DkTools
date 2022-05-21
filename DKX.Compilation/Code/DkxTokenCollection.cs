using System;
using System.Collections;
using System.Collections.Generic;

namespace DKX.Compilation.Code
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
