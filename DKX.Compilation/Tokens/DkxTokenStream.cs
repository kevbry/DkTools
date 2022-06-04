using System;

namespace DKX.Compilation.Tokens
{
    public class DkxTokenStream
    {
        private DkxTokenCollection _tokens;
        private int _pos;

        public DkxTokenStream(DkxTokenCollection tokens, int position = 0)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _pos = position;
        }

        public bool EndOfStream => _pos >= _tokens.Count;
        public int Length => _tokens.Count;
        public DkxTokenCollection RemainingTokens => _tokens.GetRange(_pos);
        public DkxTokenCollection Tokens => _tokens;

        public override string ToString() => _tokens.ToString();

        public int Position
        {
            get => _pos;
            set
            {
                if (value < 0 || value > _tokens.Count) throw new ArgumentOutOfRangeException();
                _pos = value;
            }
        }

        public DkxToken this[int index] => _tokens[index];

        public DkxToken Read()
        {
            if (_pos < _tokens.Count) return _tokens[_pos++];
            return default;
        }

        public DkxToken Peek()
        {
            if (_pos < _tokens.Count) return _tokens[_pos];
            return default;
        }

        public DkxToken Peek(int offset)
        {
            var pos = _pos + offset;
            if (pos < 0 || pos >= _tokens.Count) return default;
            return _tokens[pos];
        }

        public bool Test(Func<DkxToken,bool> callback)
        {
            if (_pos >= _tokens.Count) return false;
            return callback(_tokens[_pos]);
        }

        public int Find(Func<DkxToken, bool> callback)
        {
            var pos = _pos;

            while (pos < _tokens.Count)
            {
                if (callback(_tokens[pos])) return pos;
                pos++;
            }

            return -1;
        }

        public int Find(Func<DkxToken, int, bool> callback)
        {
            var pos = _pos;

            while (pos < _tokens.Count)
            {
                if (callback(_tokens[pos], pos)) return pos;
                pos++;
            }

            return -1;
        }

        public DkxTokenCollection GetRange(int start, int length = -1) => _tokens.GetRange(start, length);
    }
}
