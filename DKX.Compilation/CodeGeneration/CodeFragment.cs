using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using System;

namespace DKX.Compilation.CodeGeneration
{
    struct CodeFragment
    {
        private string _text;
        private DataType _dataType;
        private OpPrec _precedence;
        private CodeSpan _sourceSpan;
        private bool _readOnly;

        public static readonly CodeFragment Empty = default;

        public CodeFragment(string text, DataType dataType, OpPrec precedence, CodeSpan sourceSpan, bool readOnly)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _dataType = dataType;
            _precedence = precedence;
            _sourceSpan = sourceSpan;
            _readOnly = readOnly;
        }

        public DataType DataType => _dataType;
        public bool IsEmpty => _text == null;
        public OpPrec Precedence => _precedence;
        public bool ReadOnly => _readOnly;
        public CodeSpan SourceSpan => _sourceSpan;
        public string Text => _text;

        public override string ToString() => _text;

        public CodeFragment Protect(OpPrec fromPrecedence)
        {
            if (_precedence != OpPrec.None && _precedence < fromPrecedence)
            {
                return new CodeFragment(string.Concat("(", _text, ")"), _dataType, OpPrec.None, _sourceSpan, _readOnly);
            }
            return this;
        }
    }
}
