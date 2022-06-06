using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using System;
using System.Collections.Generic;
using System.IO;

namespace DKX.Compilation.Tokens
{
    public struct DkxToken
    {
        private DkxTokenType _type;
        private Span _span;
        private string _text;
        private decimal _number;
        private DataType _dataType;
        private bool _hasError;
        private DkxTokenCollection _tokens;

        public static readonly DkxToken None = default;
        public static readonly DkxToken[] EmptyArray = new DkxToken[0];

        private DkxToken(DkxTokenType type, Span span, string text, decimal number, DataType dataType, bool hasError)
        {
            _type = type;
            _span = span;
            _text = text;
            _number = number;
            _hasError = false;
            _dataType = dataType;

            if (type == DkxTokenType.Brackets || type == DkxTokenType.Array || type == DkxTokenType.Scope) _tokens = new DkxTokenCollection();
            else _tokens = null;
        }

        public DataType DataType => _dataType;
        public bool HasError => _hasError;
        public bool IsArray => _type == DkxTokenType.Array;
        public bool IsBrackets => _type == DkxTokenType.Brackets;
        public bool IsChar => _type == DkxTokenType.Char;
        public bool IsDataType => _type == DkxTokenType.DataType;
        public bool IsDelimiter => _type == DkxTokenType.Delimiter;
        public bool IsGroup => _type == DkxTokenType.Brackets || _type == DkxTokenType.Array || _type == DkxTokenType.Scope;
        public bool IsNone => _type == DkxTokenType.None;
        public bool IsNumber => _type == DkxTokenType.Number;
        public bool IsScope => _type == DkxTokenType.Scope;
        public bool IsStatementEnd => _type == DkxTokenType.StatementEnd;
        public bool IsString => _type == DkxTokenType.String;
        public int Position => _span.Start;
        public Span Span => _span;
        public DkxTokenType Type => _type;

        public bool IsIdentifier() => _type == DkxTokenType.Identifier;
        public bool IsIdentifier(string name) => _type == DkxTokenType.Identifier && _text == name;
        public bool IsKeyword(string keyword) => _type == DkxTokenType.Keyword && _text == keyword;
        public bool IsOperator(Operator op) => _type == DkxTokenType.Operator && _number == (decimal)op;

        public static DkxToken CreateIdentifier(string keyword, Span span) =>
            new DkxToken(DkxTokenType.Identifier, span, keyword ?? throw new ArgumentNullException(nameof(keyword)), default, DataType.Void, hasError: false);
        public static DkxToken CreateKeyword(string keyword, Span span) =>
            new DkxToken(DkxTokenType.Keyword, span, keyword ?? throw new ArgumentNullException(nameof(keyword)), default, DataType.Void, hasError: false);
        public static DkxToken CreateDataType(DataType dataType, Span span, bool hasError) =>
            new DkxToken(DkxTokenType.DataType, span, null, default, dataType, hasError);
        public static DkxToken CreateNumber(decimal number, DataType dataType, Span span) =>
            new DkxToken(DkxTokenType.Number, span, null, number, dataType, hasError: false);
        public static DkxToken CreateString(string rawText, Span span, bool hasError) =>
            new DkxToken(DkxTokenType.String, span, rawText ?? throw new ArgumentNullException(nameof(rawText)), default, DataType.String255, hasError);
        public static DkxToken CreateChar(char ch, Span span, bool hasError) =>
            new DkxToken(DkxTokenType.Char, span, null, ch, DataType.Char, hasError);
        public static DkxToken CreateOperator(Operator op, Span span) =>
            new DkxToken(DkxTokenType.Operator, span, null, (decimal)op, DataType.Void, hasError: false);
        public static DkxToken CreateDelimiter(Span span) =>
            new DkxToken(DkxTokenType.Delimiter, span, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateStatementEnd(Span span) =>
            new DkxToken(DkxTokenType.StatementEnd, span, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateArguments(Span openSpan) =>
            new DkxToken(DkxTokenType.Brackets, openSpan, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateArray(Span openSpan) =>
            new DkxToken(DkxTokenType.Array, openSpan, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateScope(Span openSpan) =>
            new DkxToken(DkxTokenType.Scope, openSpan, null, default, DataType.Void, hasError: false);
        public static DkxToken CreateInvalid(char ch, Span span) =>
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
            _span = _span + token._span;
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
            _span = _span + endPosition;
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
                case DkxTokenType.Brackets:
                    return $"({string.Join(" ", _tokens)})";
                case DkxTokenType.Array:
                    return $"[{string.Join(", ", _tokens)}]";
                case DkxTokenType.Scope:
                    return $"{{{string.Join(" ", _tokens)}}}";
                case DkxTokenType.DataType:
                    return _dataType.ToString();
                default:
                    throw new InvalidDkxTokenTypeException();
            }
        }

        // TODO: remove
        //public void Serialize(BinaryWriter bin)
        //{
        //    bin.Write((byte)_type);
        //    _span.Serialize(bin);
        //    bin.Write(_text != null);
        //    if (_text != null) bin.Write(_text);
        //    bin.Write(_number);
        //    _dataType.Serialize(bin);
        //    bin.Write(_hasError);
        //    bin.Write(_tokens != null);
        //    if (_tokens != null) _tokens.Serialize(bin);
        //}

        //public static DkxToken Deserialize(BinaryReader bin)
        //{
        //    var typeValue = bin.ReadByte();
        //    if (!Enum.IsDefined(typeof(DkxTokenType), typeValue)) throw new InvalidBaseTypeException();
        //    var type = (DkxTokenType)typeValue;

        //    string text = null;
        //    DkxTokenCollection tokens = null;

        //    var span = Span.Deserialize(bin);
        //    if (bin.ReadBoolean()) text = bin.ReadString();
        //    var number = bin.ReadDecimal();
        //    var dataType = DataType.Deserialize(bin);
        //    var hasError = bin.ReadBoolean();
        //    if (bin.ReadBoolean()) tokens = DkxTokenCollection.Deserialize(bin);

        //    return new DkxToken(type, span, text, number, dataType, hasError) { _tokens = tokens };
        //}
    }

    public enum DkxTokenType
    {
        None = 0,
        Invalid = 1,
        Keyword = 2,
        DataType = 3,
        Identifier = 4,
        Number = 5,
        String = 6,
        Char = 7,
        Operator = 8,
        Delimiter = 9,
        StatementEnd = 10,
        Brackets = 16,
        Array = 17,
        Scope = 18
    }

    class InvalidDkxTokenTypeException : CompilerException { }
}
