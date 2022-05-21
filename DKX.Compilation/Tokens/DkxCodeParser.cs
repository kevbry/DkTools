using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DKX.Compilation.Tokens
{
    public class DkxCodeParser
    {
        private string _source;
        private int _pos;
        private int _len;
        private StringBuilder _sb = new StringBuilder();

        public DkxCodeParser(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _pos = 0;
            _len = _source.Length;
        }

        public static bool IsWhiteSpace(char ch) => ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
        public static bool IsWordChar(char ch, bool first) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_' || (!first && ch >= '0' && ch <= '9');
        public static bool IsDigit(char ch) => ch >= '0' && ch <= '9';
        public static bool IsHexDigit(char ch) => (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
        public static int HexDigitToInt(char ch) => ch >= '0' && ch <= '9' ? ch - '0' : ch >= 'A' && ch <= 'F' ? ch - 'A' + 0x0A : ch >= 'a' && ch <= 'f' ? ch - 'a' + 0x0a : 0;

        public bool EndOfFile => _pos >= _len;
        public string RemainingSource => _source.Substring(_pos);
        public string Source => _source;

        public void SkipWhiteSpace()
        {
            while (_pos < _len)
            {
                var ch = _source[_pos];
                if (IsWhiteSpace(ch))
                {
                    _pos++;
                    continue;
                }

                if (ch == '/' && _pos + 1 < _len)
                {
                    ch = _source[_pos + 1];
                    if (ch == '/')
                    {
                        // Single-line comment
                        var end = _source.IndexOf('\n', _pos);
                        if (end < 0) _pos = _len;
                        else _pos = end + 1;
                        continue;
                    }
                    else if (ch == '*')
                    {
                        // Multi-line comment
                        var end = _source.IndexOf("*/", _pos + 2);
                        if (end < 0) _pos = _len;
                        else _pos = end + 2;
                        continue;
                    }
                }

                break;
            }
        }

        public DkxToken ReadAll()
        {
            var root = DkxToken.CreateScope(CodeSpan.Empty);
            while (TryRead(out var token)) root.Add(token);
            return root;
        }

        private static readonly Regex _numericDataTypeRegex = new Regex(@"^(numeric|unsigned)(\d{1,2})$");
        private static readonly Regex _stringDataTypeRegex = new Regex(@"^string(\d{1,3})$");

        public bool TryRead(out DkxToken tokenOut)
        {
            SkipWhiteSpace();
            if (_pos >= _len)
            {
                tokenOut = default;
                return false;
            }

            var startPos = _pos;
            var ch = _source[_pos];
            switch (ch)
            {
                case '+':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.AssignAdd, new CodeSpan(startPos, ++_pos));
                    else if (_pos < _len && _source[_pos] == ch) tokenOut = DkxToken.CreateOperator(Operator.Increment, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.Add, new CodeSpan(startPos, _pos));
                    return true;
                case '-':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.AssignSubtract, new CodeSpan(startPos, ++_pos));
                    else if (_pos < _len && _source[_pos] == ch) tokenOut = DkxToken.CreateOperator(Operator.Decrement, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.Subtract, new CodeSpan(startPos, _pos));
                    return true;
                case '*':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.AssignMultiply, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.Multiply, new CodeSpan(startPos, _pos));
                    return true;
                case '/':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.AssignDivide, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.Divide, new CodeSpan(startPos, _pos));
                    return true;
                case '%':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.AssignModulus, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.Modulus, new CodeSpan(startPos, _pos));
                    return true;
                case '=':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.Equal, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.Assign, new CodeSpan(startPos, _pos));
                    return true;
                case '!':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.NotEqual, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.Not, new CodeSpan(startPos, _pos));
                    return true;
                case '<':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.LessEqual, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.LessThan, new CodeSpan(startPos, _pos));
                    return true;
                case '>':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '=') tokenOut = DkxToken.CreateOperator(Operator.GreaterEqual, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateOperator(Operator.GreaterThan, new CodeSpan(startPos, _pos));
                    return true;
                case '.':
                    tokenOut = DkxToken.CreateOperator(Operator.Dot, new CodeSpan(startPos, ++_pos));
                    return true;
                case ',':
                    tokenOut = DkxToken.CreateDelimiter(new CodeSpan(startPos, ++_pos));
                    return true;
                case ';':
                    tokenOut = DkxToken.CreateStatementEnd(new CodeSpan(startPos, ++_pos));
                    return true;
                case '(':
                    tokenOut = DkxToken.CreateArguments(new CodeSpan(startPos, ++_pos));
                    ReadNestable(ref tokenOut, ')');
                    return true;
                case '[':
                    tokenOut = DkxToken.CreateArray(new CodeSpan(startPos, ++_pos));
                    ReadNestable(ref tokenOut, ']');
                    return true;
                case '{':
                    tokenOut = DkxToken.CreateScope(new CodeSpan(startPos, ++_pos));
                    ReadNestable(ref tokenOut, '}');
                    return true;
                case '"':
                    ReadStringLiteral(out tokenOut);
                    return true;
                case '\'':
                    ReadCharLiteral(out tokenOut);
                    return true;
                case '@':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '"') ReadVerbatimString(out tokenOut);
                    else tokenOut = DkxToken.CreateInvalid('@', new CodeSpan(startPos, _pos));
                    return true;
                case '&':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '&') tokenOut = DkxToken.CreateOperator(Operator.And, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateInvalid('&', new CodeSpan(startPos, _pos));
                    return true;
                case '|':
                    _pos++;
                    if (_pos < _len && _source[_pos] == '|') tokenOut = DkxToken.CreateOperator(Operator.Or, new CodeSpan(startPos, ++_pos));
                    else tokenOut = DkxToken.CreateInvalid('|', new CodeSpan(startPos, _pos));
                    return true;
                case '?':
                    tokenOut = DkxToken.CreateOperator(Operator.Ternary1, new CodeSpan(startPos, ++_pos));
                    return true;
                case ':':
                    tokenOut = DkxToken.CreateOperator(Operator.Ternary2, new CodeSpan(startPos, ++_pos));
                    return true;

                default:
                    if (IsWordChar(ch, true))
                    {
                        _sb.Clear();
                        while (_pos < _len && IsWordChar(ch = _source[_pos], false))
                        {
                            _sb.Append(ch);
                            _pos++;
                        }

                        var word = _sb.ToString();
                        var span = new CodeSpan(startPos, _pos);

                        // Numeric types are defined as: numeric9  numeric11.2  unsigned19  unsigned12.2
                        Match match;
                        if ((match = _numericDataTypeRegex.Match(word)).Success)
                        {
                            var keyword = match.Groups[1].Value;
                            var width = int.Parse(match.Groups[2].Value);
                            if (width >= DkxConst.Numeric.MinWidth && width <= DkxConst.Numeric.MaxWidth)
                            {
                                if (_pos < _len && _source[_pos] == '.')
                                {
                                    _pos++;
                                    var scale = 0;
                                    while (_pos < _len && IsDigit(_source[_pos])) { scale *= 10; scale += _source[_pos++] - '0'; }
                                    if (scale >= DkxConst.Numeric.MinScale && scale <= DkxConst.Numeric.MaxScale && scale <= width)
                                    {
                                        tokenOut = DkxToken.CreateDataType(new DataType(keyword == "unsigned" ? BaseType.UNumeric : BaseType.Numeric, width: (byte)width, scale: (byte)scale), new CodeSpan(startPos, _pos));
                                        return true;
                                    }
                                    else
                                    {
                                        // Scale is out of bounds. Fall back to just the width.
                                        _pos = span.End;
                                    }
                                }
                                else
                                {
                                    tokenOut = DkxToken.CreateDataType(new DataType(keyword == "unsigned" ? BaseType.UNumeric : BaseType.Numeric, width: (byte)width, scale: 0), span);
                                    return true;
                                }
                            }
                            else
                            {
                                tokenOut = DkxToken.CreateDataType(new DataType(keyword == "unsigned" ? BaseType.UNumeric : BaseType.Numeric, width: (byte)width, scale: 0), span);
                                return true;
                            }
                        }
                        else if ((match = _stringDataTypeRegex.Match(word)).Success)
                        {
                            var width = int.Parse(match.Groups[1].Value);
                            if (width <= DkxConst.String.MaxLength)
                            {
                                tokenOut = DkxToken.CreateDataType(new DataType(BaseType.String, width: (byte)width), span);
                                return true;
                            }
                        }

                        switch (word)
                        {
                            case DkxConst.Keywords.Void:
                                tokenOut = DkxToken.CreateDataType(DataType.Void, span);
                                return true;
                            case DkxConst.Keywords.Bool:
                                tokenOut = DkxToken.CreateDataType(DataType.Bool, span);
                                return true;
                            case DkxConst.Keywords.Char:
                                tokenOut = DkxToken.CreateDataType(DataType.Char, span);
                                return true;
                            case DkxConst.Keywords.Date:
                                tokenOut = DkxToken.CreateDataType(DataType.Date, span);
                                return true;
                            case DkxConst.Keywords.Time:
                                tokenOut = DkxToken.CreateDataType(DataType.Time, span);
                                return true;
                            case DkxConst.Keywords.Int:
                                tokenOut = DkxToken.CreateDataType(DataType.Int, span);
                                return true;
                            case DkxConst.Keywords.UInt:
                                tokenOut = DkxToken.CreateDataType(DataType.UInt, span);
                                return true;
                            case DkxConst.Keywords.Short:
                                tokenOut = DkxToken.CreateDataType(DataType.Short, span);
                                return true;
                            case DkxConst.Keywords.UShort:
                                tokenOut = DkxToken.CreateDataType(DataType.UShort, span);
                                return true;
                            case DkxConst.Keywords.String:
                                tokenOut = DkxToken.CreateDataType(DataType.String255, span);
                                return true;
                            case DkxConst.Keywords.Variant:
                                tokenOut = DkxToken.CreateDataType(DataType.Variant, span);
                                return true;
                        }

                        if (DkxConst.Keywords.AllKeywords.Contains(word)) tokenOut = DkxToken.CreateKeyword(word, span);
                        else tokenOut = DkxToken.CreateIdentifier(word, span);
                        return true;
                    }

                    if (TryPeekNumericStart())
                    {
                        ReadNumeric(out tokenOut);
                        return true;
                    }

                    tokenOut = DkxToken.CreateInvalid(ch, new CodeSpan(_pos, ++_pos));
                    return true;
            }
        }

        private void ReadNestable(ref DkxToken container, char endChar)
        {
            while (true)
            {
                SkipWhiteSpace();
                if (_pos < _len && _source[_pos] == endChar)
                {
                    container.OnClosed(++_pos);
                    break;
                }

                if (TryRead(out var token)) container.Add(token);
                else break;
            }
        }

        private bool TryPeekNumericStart()
        {
            if (_pos >= _len) return false;

            var ch = _source[_pos];
            return IsDigit(ch) ||
                (ch == '-' && _pos + 1 < _len && IsDigit(_source[_pos + 1])) ||
                (ch == '.' && _pos + 1 < _len && IsDigit(_source[_pos + 1])) ||
                (ch == '-' && _pos + 2 < _len && _source[_pos + 1] == '.' && IsDigit(_source[_pos + 1]));
        }

        private void ReadNumeric(out DkxToken tokenOut)
        {
            // This function is only entered if TryPeekNumericStart() has returned true.

            char ch;
            decimal value = 0;
            byte width = 0;

            var startPos = _pos;
            if (_pos + 2 < _len && _source[_pos + 1] == 'x' && IsHexDigit(_source[_pos + 2]))
            {
                // Hex number

                _pos += 2;
                while (IsHexDigit(ch = _source[_pos]))
                {
                    value *= 16;
                    value += HexDigitToInt(ch);
                    width++;
                    _pos++;
                }

                DataType dataType;
                if (value >= int.MinValue && value <= int.MaxValue) dataType = DataType.Int;
                else if (value >= 0 && value <= uint.MaxValue) dataType = DataType.UInt;
                else dataType = new DataType(value >= 0 ? BaseType.UNumeric : BaseType.Numeric, width: width, scale: 0);

                tokenOut = DkxToken.CreateNumber(value, dataType, new CodeSpan(startPos, _pos));
            }
            else
            {
                // Decimal number

                var gotDot = false;
                byte scale = 0;

                _sb.Clear();
                while (_pos < _len)
                {
                    ch = _source[_pos];
                    if (ch == '-' && _pos == startPos)
                    {
                        _sb.Append('-');
                        _pos++;
                    }
                    else if (ch == '.' && gotDot == false)
                    {
                        gotDot = true;
                        _sb.Append('.');
                        _pos++;
                    }
                    else if (IsDigit(ch))
                    {
                        _sb.Append(ch);
                        if (gotDot) scale++;
                        width++;
                        _pos++;
                    }
                    else break;
                }

                value = decimal.Parse(_sb.ToString());
                var dataType = ReadNumericSuffix(value, width, scale);
                tokenOut = DkxToken.CreateNumber(value, dataType, new CodeSpan(startPos, _pos));
            }
        }

        private DataType ReadNumericSuffix(decimal value, int width, int scale)
        {
            if (_pos >= _len)
            {
                switch (_source[_pos])
                {
                    case 'L':
                        _pos++;
                        return DataType.Int;

                    case 'U':
                        _pos++;
                        return DataType.UInt;

                    case 'M':
                        _pos++;
                        var resetPos = _pos;
                        if (_pos + 1 < _len && IsDigit(_source[_pos + 1]))
                        {
                            var specificScale = 0;
                            while (IsDigit(_source[_pos]))
                            {
                                specificScale *= 10;
                                specificScale += _source[_pos] - '0';
                            }
                            var specificWidth = specificScale / 100;
                            specificWidth %= 100;

                            if (specificScale >= DkxConst.Numeric.MinScale && specificScale <= DkxConst.Numeric.MaxScale &&
                                specificWidth >= DkxConst.Numeric.MinWidth && specificWidth <= DkxConst.Numeric.MaxWidth)
                            {
                                return new DataType(value >= 0 ? BaseType.UNumeric : BaseType.Numeric, width: (byte)specificWidth, scale: (byte)specificScale);
                            }
                            else
                            {
                                _pos = resetPos;
                            }
                        }

                        return new DataType(value >= 0 ? BaseType.UNumeric : BaseType.Numeric, width: (byte)width, scale: (byte)scale);
                    
                }
            }

            if (value >= int.MinValue && value <= int.MaxValue) return DataType.Int;
            if (value >= 0 && value <= uint.MaxValue) return DataType.UInt;
            return new DataType(value >= 0 ? BaseType.UNumeric : BaseType.Numeric, width: (byte)width, scale: (byte)scale);
        }

        private void ReadStringLiteral(out DkxToken tokenOut)
        {
            _sb.Clear();
            if (_pos >= _len || _source[_pos] != '\"') throw new InvalidOperationException("Current char is not a quote.");

            var startPos = _pos++;
            var closed = false;
            var hasError = false;

            while (_pos < _len)
            {
                var ch = _source[_pos];
                if (ch == '\"')
                {
                    closed = true;
                    _pos++;
                    break;
                }

                if (ch == '\r' || ch == '\n')
                {
                    hasError = true;
                    break;
                }

                if (ch == '\\' && _pos + 1 < _len)
                {
                    _pos++;
                    _sb.Append(UnescapeChar(ref hasError));
                    continue;
                }

                _sb.Append(ch);
                _pos++;
            }

            if (!closed) hasError = true;

            tokenOut = DkxToken.CreateString(_sb.ToString(), new CodeSpan(startPos, _pos), hasError);
        }

        private void ReadVerbatimString(out DkxToken tokenOut)
        {
            // This function assumes the '@' has already been read, and is currently sitting on the opening quote.
            _sb.Clear();
            if (_pos >= _len || _source[_pos] != '\"') throw new InvalidOperationException("Current char is not a quote.");

            var startPos = (_pos++) - 1;
            var closed = false;

            while (_pos < _len)
            {
                var ch = _source[_pos];
                if (ch == '"')
                {
                    _pos++;
                    if (_pos < _len && _source[_pos] == '"')
                    {
                        _sb.Append('"');
                        _pos++;
                    }
                    else
                    {
                        closed = true;
                        break;
                    }
                }
                else
                {
                    _sb.Append(ch);
                    _pos++;
                }
            }

            tokenOut = DkxToken.CreateString(_sb.ToString(), new CodeSpan(startPos, _pos), !closed);
        }

        private void ReadCharLiteral(out DkxToken tokenOut)
        {
            _sb.Clear();

            // Read the open quote
            if (_pos >= _len || _source[_pos] != '\'') throw new InvalidOperationException("Current char is not a quote.");
            var startPos = _pos++;

            // Read the character
            if (_pos >= _len)
            {
                tokenOut = DkxToken.CreateChar(default, new CodeSpan(startPos, _pos), hasError: true);
                return;
            }

            var ch = _source[_pos];
            var hasError = false;
            if (ch == '\\')
            {
                ch = UnescapeChar(ref hasError);
            }
            else if (ch == '\'')
            {
                // Empty literal
                ch = default;
                hasError = true;
                _pos++;
            }
            else if (ch == '\r' || ch == '\n')
            {
                // New line interrupts literal
                ch = default;
                hasError = true;
            }
            else
            {
                _pos++;
                if (_pos < _len && _source[_pos] == '\'')
                {
                    // Closed cleanly
                    _pos++;
                    tokenOut = DkxToken.CreateChar(ch, new CodeSpan(startPos, _pos), hasError);
                    return;
                }

                // Either unterminated or contains multiple chars
                // Read until either the end is found, a new line breaks it, or end of file.
                while (_pos < _len)
                {
                    switch (_source[_pos])
                    {
                        case '\'':
                            _pos++;
                            tokenOut = DkxToken.CreateChar(ch, new CodeSpan(startPos, _pos), true);
                            return;
                        case '\\':
                            _pos++;
                            UnescapeChar(ref hasError);
                            break;
                        case '\r':
                        case '\n':
                            tokenOut = DkxToken.CreateChar(ch, new CodeSpan(startPos, _pos), true);
                            return;
                        default:
                            _pos++;
                            break;
                    }
                }
            }

            tokenOut = DkxToken.CreateChar(ch, new CodeSpan(startPos, _pos), true);
        }

        private char UnescapeChar(ref bool hasError)
        {
            // This function assumes the position has already been advanced past the '\' and is now sitting on the next character.

            switch (_source[_pos])
            {
                case '\'':  // Single quote
                    _pos++;
                    return '\'';
                case '"':   // Double quote
                    _pos++;
                    return '"';
                case '\\':  // Backslash
                    _pos++;
                    return '\\';
                case '0':   // Null
                    _pos++;
                    return '\0';
                case 'a':   // Alert
                    _pos++;
                    return '\a';
                case 'b':   // Backspace
                    _pos++;
                    return '\b';
                case 'f':   // Form feed
                    _pos++;
                    return '\f';
                case 'n':   // New line
                    _pos++;
                    return '\n';
                case 'r':   // Carriage return
                    _pos++;
                    return '\r';
                case 't':   // Horizontal tab
                    _pos++;
                    return '\t';
                case 'v':   // Vertical tab
                    _pos++;
                    return '\v';
                case 'u':   // Other code (8-bit)
                    if (_pos + 2 < _len &&
                        IsHexDigit(_source[_pos + 1]) &&
                        IsHexDigit(_source[_pos + 2]))
                    {
                        var ch = (char)((HexDigitToInt(_source[_pos + 1]) << 4) |
                            HexDigitToInt(_source[_pos + 2]));
                        _pos += 3;
                        return ch;
                    }
                    else
                    {
                        hasError = true;
                        return '\\';
                    }
                case 'U':   // Other code (16-bit)
                    if (_pos + 4 < _len &&
                        IsHexDigit(_source[_pos + 1]) &&
                        IsHexDigit(_source[_pos + 2]) &&
                        IsHexDigit(_source[_pos + 3]) &&
                        IsHexDigit(_source[_pos + 4]))
                    {
                        var ch = (char)((HexDigitToInt(_source[_pos + 1]) << 12) |
                            (HexDigitToInt(_source[_pos + 2]) << 8) |
                            (HexDigitToInt(_source[_pos + 3]) << 4) |
                            HexDigitToInt(_source[_pos + 4]));
                        _pos += 5;
                        return ch;
                    }
                    else
                    {
                        hasError = true;
                        return '\\';
                    }
                default:
                    hasError = true;
                    return '\\';
            }
        }

        private static void EscapeChar(char ch, StringBuilder sb)
        {
            switch (ch)
            {
                case '\'':
                    sb.Append("\\'");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\0':
                    sb.Append("\\0");
                    break;
                case '\a':
                    sb.Append("\\a");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\v':
                    sb.Append("\\v");
                    break;
                default:
                    if (ch < ' ' || ch >= '~')
                    {
                        if (ch <= 0xff) sb.AppendFormat("\\u{0:X2}", (int)ch);
                        else sb.AppendFormat("\\u{0:X4}", (int)ch);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }

        public static string StringToStringLiteral(string str)
        {
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (var ch in str) EscapeChar(ch, sb);
            sb.Append('"');
            return sb.ToString();
        }

        public static string CharToCharLiteral(char ch)
        {
            var sb = new StringBuilder();
            sb.Append('\'');
            EscapeChar(ch, sb);
            sb.Append('\'');
            return sb.ToString();
        }

        public static string StringLiteralToString(string literalText)
        {
            if (string.IsNullOrEmpty(literalText) || !literalText.StartsWith("\"")) throw new ArgumentException("Text does not contain a string literal.");

            var cp = new DkxCodeParser(literalText);
            cp.ReadStringLiteral(out var token);
            return token.Text;
        }
    }
}
