using System;

namespace DKX.Compilation.Tokens
{
    public class DkxTokenStream
    {
        private DkxTokenCollection _tokens;
        private int _pos;

        public DkxTokenStream(DkxTokenCollection tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _pos = 0;
        }

        public bool EndOfStream => _pos >= _tokens.Count;

        public int Position
        {
            get => _pos;
            set
            {
                if (value < 0 || value > _tokens.Count) throw new ArgumentOutOfRangeException();
                _pos = value;
            }
        }

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

        public bool Test(Func<DkxToken,bool> callback)
        {
            if (_pos >= _tokens.Count) return false;
            return callback(_tokens[_pos]);
        }
    }
}
