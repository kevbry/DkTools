using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Variables.ConstantValues;
using System;

namespace DKX.Compilation.CodeGeneration
{
    struct CodeFragment
    {
        private string _text;
        private DataType _dataType;
        private OpPrec _precedence;
        private Span _span;
        private ConstValue _constant;
        private CodeFragmentFlags _flags;

        public static readonly CodeFragment Empty = default;

        public CodeFragment(string text, DataType dataType, OpPrec precedence, Span span, bool reportable, CodeFragmentFlags flags = default, ConstValue constant = null)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _dataType = dataType;
            _precedence = precedence;
            _span = span;
            _constant = constant;
            _flags = flags;
            if (reportable) _flags |= CodeFragmentFlags.Reportable;
        }

        public ConstValue Constant => _constant;
        public DataType DataType => _dataType;
        public CodeFragmentFlags Flags => _flags;
        public bool IsConstant => _constant != null;
        public bool IsEmpty => _text == null;
        /// <summary>
        /// If this code fragment is executed as-is, it will result in output to the report stream.
        /// </summary>
        public bool IsReportable => _flags.HasFlag(CodeFragmentFlags.Reportable);
        /// <summary>
        /// The object reference in this fragment is a temporary value and needs to be owned by something.
        /// If the value is dropped, it must be decremented; otherwise a memory leak will occur.
        /// </summary>
        public bool IsUnownedObjectReference => _dataType.IsClass && _flags.HasFlag(CodeFragmentFlags.UnownedObjectReference);
        public OpPrec Precedence => _precedence;
        public Span SourceSpan => _span;
        public string Text => _text;

        public override string ToString() => _text;

        public CodeFragment Protect(OpPrec fromPrecedence)
        {
            if (_precedence != OpPrec.None && _precedence < fromPrecedence)
            {
                return new CodeFragment(string.Concat("(", _text, ")"), _dataType, OpPrec.None, _span, reportable: false, flags: _flags, constant: _constant);
            }
            return this;
        }
    }

    [Flags]
    enum CodeFragmentFlags
    {
        /// <summary>
        /// The object reference in this fragment is a temporary value and needs to be owned by something.
        /// If the value is dropped, it must be decremented; otherwise a memory leak will occur.
        /// </summary>
        UnownedObjectReference = 0x01,

        /// <summary>
        /// If this code fragment is executed as-is, it will result in output to the report stream.
        /// </summary>
        Reportable = 0x02,
    }

    static class CodeFragmentFlagsHelper
    {
        public static bool IsUnownedObjectReference(this CodeFragmentFlags flags) => flags.HasFlag(CodeFragmentFlags.UnownedObjectReference);
    }
}
