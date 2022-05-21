using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Tokens
{
    public struct DkxToken
    {
        private DkxTokenType _type;
        private CodeSpan _span;
        private string _text;
        private decimal _number;
        private DataType _dataType;
        private bool _hasError;
        private DkxTokenCollection _tokens;

        public static readonly DkxToken[] EmptyArray = new DkxToken[0];

        private DkxToken(DkxTokenType type, CodeSpan span, string text, decimal number, DataType dataType, bool hasError)
        {
            _type = type;
            _span = span;
            _text = text;
            _number = number;
            _hasError = false;
            _dataType = dataType;

            if (type == DkxTokenType.Arguments || type == DkxTokenType.Array || type == DkxTokenType.Scope) _tokens = new DkxTokenCollection();
            else _tokens = null;
        }

        public DataType DataType => _dataType;
        public bool HasError => _hasError;
        public bool IsGroup => _type == DkxTokenType.Arguments || _type == DkxTokenType.Array || _type == DkxTokenType.Scope;
        public bool IsNone => _type == DkxTokenType.None;
        public int Position => _span.Start;
        public CodeSpan Span => _span;
        public DkxTokenType Type => _type;

        public static DkxToken CreateIdentifier(string keyword, CodeSpan span) =>
            new DkxToken(DkxTokenType.Identifier, span, keyword ?? throw new ArgumentNullException(nameof(keyword)), default, DataType.Void, hasError: false);
        public static DkxToken CreateKeyword(string keyword, CodeSpan span) =>
            new DkxToken(DkxTokenType.Keyword, span, keyword ?? throw new ArgumentNullException(nameof(keyword)), default, DataType.Void, hasError: false);
        public static DkxToken CreateDataType(DataType dataType, CodeSpan span) =>
            new DkxToken(DkxTokenType.DataType, span, null, default, dataType, hasError: false);
        public static DkxToken CreateNumber(decimal number, DataType dataType, CodeSpan span) =>
            new DkxToken(DkxTokenType.Number, span, null, number, dataType, hasError: false);
        public static DkxToken CreateString(string rawText, CodeSpan span, bool hasError) =>
            new DkxToken(DkxTokenType.String, span, rawText ?? throw new ArgumentNullException(nameof(rawText)), default, DataType.String255, hasError);
        public static DkxToken CreateChar(char ch, CodeSpan span, bool hasError) =>
            new DkxToken(DkxTokenType.Char, span, null, ch, DataType.Char, hasError);
        public static DkxToken CreateOperator(Operator op, CodeSpan span) =>
            new DkxToken(DkxTokenType.Operator, span, null, (decimal)op, DataType.Void, hasError: false);
        public static DkxToken CreateDelimiter(CodeSpan span) =>
            new DkxToken(DkxTokenType.Delimiter, span, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateStatementEnd(CodeSpan span) =>
            new DkxToken(DkxTokenType.StatementEnd, span, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateArguments(CodeSpan openSpan) =>
            new DkxToken(DkxTokenType.Arguments, openSpan, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateArray(CodeSpan openSpan) =>
            new DkxToken(DkxTokenType.Array, openSpan, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateScope(CodeSpan openSpan) =>
            new DkxToken(DkxTokenType.Scope, openSpan, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateInvalid(char ch, CodeSpan span) =>
            new DkxToken(DkxTokenType.Invalid, span, null, ch, DataType.Void, hasError: false);

        public decimal Number
        {
            get
            {
#if DEBUG
                if (_type != DkxTokenType.Number) throw new InvalidOperationException("Cannot pull a number out of a token that is not a number.");
#endif
                return _number;
            }
        }

        public string Text
        {
            get
            {
#if DEBUG
                switch (_type)
                {
                    case DkxTokenType.Keyword:
                    case DkxTokenType.DataType:
                    case DkxTokenType.Identifier:
                    case DkxTokenType.String:
                        break;
                    default:
                        throw new InvalidOperationException("Cannot pull a string out of this token.");
                }
#endif
                return _text;
            }
        }

        public char Char
        {
            get
            {
#if DEBUG
                switch (_type)
                {
                    case DkxTokenType.Char:
                    case DkxTokenType.Invalid:
                        break;
                    default:
                        throw new InvalidOperationException("Cannot pull a char out of this token.");
                }
#endif
                return (char)_number;
            }
        }

        public Operator Operator
        {
            get
            {
#if DEBUG
                if (_type != DkxTokenType.Operator) throw new InvalidOperationException("Cannot pull an operator out of this token.");
#endif
                return (Operator)_number;
            }
        }

        public void Add(DkxToken token)
        {
#if DEBUG
            if (!IsGroup) throw new InvalidOperationException("Child tokens cannot be added to a token that is not a group.");
#endif
            if (_tokens == null) _tokens = new DkxTokenCollection();
            _tokens.Add(token);
            _span = _span.Envelope(token._span);
        }

        public void AddRange(IEnumerable<DkxToken> tokens)
        {
            foreach (var token in tokens) Add(token);
        }

        public void OnClosed(int endPosition)
        {
#if DEBUG
            if (!IsGroup) throw new InvalidOperationException("Cannot close a token that is not a group.");
            if (_number != 0) throw new InvalidOperationException("Token is already closed.");
#endif
            _number = 1;
            _span = new CodeSpan(_span.Start, endPosition);
        }

        public bool Closed
        {
            get
            {
#if DEBUG
                if (!IsGroup) throw new InvalidOperationException("Cannot get closed flag for a token that is not a group.");
#endif
                return _number != 0;
            }
        }

        public DkxTokenCollection Tokens
        {
            get
            {
#if DEBUG
                if (!IsGroup) throw new InvalidOperationException("Cannot get child tokens for a token that is not a group.");
#endif
                return _tokens;
            }
        }

        public override string ToString()
        {
            switch (_type)
            {
                case DkxTokenType.None:
                    return "(none)";
                case DkxTokenType.Invalid:
                    return DkxCodeParser.CharToCharLiteral((char)_number);
                case DkxTokenType.Keyword:
                case DkxTokenType.Identifier:
                    return _text;
                case DkxTokenType.Number:
                    return _number.ToString();
                case DkxTokenType.String:
                    return DkxCodeParser.StringToStringLiteral(_text);
                case DkxTokenType.Char:
                    return DkxCodeParser.CharToCharLiteral((char)_number);
                case DkxTokenType.Operator:
                    return ((Operator)_number).GetText();
                case DkxTokenType.Delimiter:
                    return ",";
                case DkxTokenType.StatementEnd:
                    return ";";
                case DkxTokenType.Arguments:
                    return $"({string.Join(" ", _tokens)})";
                case DkxTokenType.Array:
                    return $"[{string.Join(", ", _tokens)}]";
                case DkxTokenType.Scope:
                    return $"{{{string.Join(" ", _tokens)}}}";
                default:
                    return string.Empty;
            }
        }
    }

    public enum DkxTokenType
    {
        None,
        Invalid,
        Keyword,
        DataType,
        Identifier,
        Number,
        String,
        Char,
        Operator,
        Delimiter,
        StatementEnd,
        Arguments,
        Array,
        Scope
    }
}
