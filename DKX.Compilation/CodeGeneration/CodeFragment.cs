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
        private bool _terminated;
        private CodeSpan _sourceSpan;
        private bool _readOnly;

        public static readonly CodeFragment Empty = default;

        public CodeFragment(string text, DataType dataType, OpPrec precedence, bool terminated, CodeSpan sourceSpan, bool readOnly)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _dataType = dataType;
            _precedence = precedence;
            _terminated = terminated;
            _sourceSpan = sourceSpan;
            _readOnly = readOnly;
        }

        public DataType DataType => _dataType;
        public bool IsEmpty => _text == null;
        public OpPrec Precedence => _precedence;
        public bool ReadOnly => _readOnly;
        public CodeSpan SourceSpan => _sourceSpan;
        public bool Terminated => _terminated;
        public string Text => _text;

        public override string ToString() => _text;

        public CodeFragment Protect(OpPrec fromPrecedence)
        {
            if (_precedence != OpPrec.None && _precedence < fromPrecedence)
            {
                return new CodeFragment(string.Concat("(", _text, ")"), _dataType, OpPrec.None, _terminated, _sourceSpan, _readOnly);
            }
            return this;
        }
    }
}
