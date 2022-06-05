using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Variables.ConstantValues;
using System;

namespace DKX.Compilation.CodeGeneration
{
    public struct CodeFragment
    {
        private string _text;
        private DataType _dataType;
        private OpPrec _precedence;
        private Span _span;
        private ConstValue _constant;

        public static readonly CodeFragment Empty = default;

        public CodeFragment(string text, DataType dataType, OpPrec precedence, Span span, ConstValue constant = null)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _dataType = dataType;
            _precedence = precedence;
            _span = span;
            _constant = constant;
        }

        public ConstValue Constant => _constant;
        public DataType DataType => _dataType;
        public bool IsConstant => _constant != null;
        public bool IsEmpty => _text == null;
        public OpPrec Precedence => _precedence;
        public Span SourceSpan => _span;
        public string Text => _text;

        public override string ToString() => _text;

        public CodeFragment Protect(OpPrec fromPrecedence)
        {
            if (_precedence != OpPrec.None && _precedence < fromPrecedence)
            {
                return new CodeFragment(string.Concat("(", _text, ")"), _dataType, OpPrec.None, _span);
            }
            return this;
        }
    }
}
