using DK;
using DK.Code;
using DKX.Compilation.Exceptions;
using System;
using System.Text;

namespace DKX.Compilation.CodeGeneration.OpCodes
{
    public class OpCodeParser
    {
        public const string True = "true";
        public const string False = "false";

        private string _source;
        private int _pos;
        private int _len;
        private StringBuilder _text = new StringBuilder();
        private CodeSpan _span;
        private int _spanOffset;

        public OpCodeParser(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _pos = 0;
            _len = _source.Length;
        }

        public int Position { get => _pos; set => _pos = value; }
        public string RemainingSource => _source.Substring(_pos);
        public string Source => _source;
        public CodeSpan Span => _span;
        public string Text => _text.ToString();

        public int SpanOffset { get => _spanOffset; set => _spanOffset = value; }

        public bool EndOfFile => _pos >= _len;

        public OpCodeType Read()
        {
            while (_pos >= _len) return OpCodeType.None;

            _text.Clear();
            var ch = _source[_pos];

            if (IsAlpha(ch))
            {
                while (_pos < _len && IsAlpha(ch = _source[_pos]))
                {
                    _text.Append(ch);
                    _pos++;
                }
                ReadSpanSuffix();
                return OpCodeType.OpCode;
            }

            if (ch == '$')
            {
                _pos++;
                if (_pos > _len || !(ch = _source[_pos]).IsWordChar(true)) throw new InvalidOpCodeSourceException("'$' not followed by word char.");
                _text.Append(ch);

                while ((ch = _source[++_pos]).IsWordChar(false)) _text.Append(ch);

                ReadSpanSuffix();
                return OpCodeType.Variable;
            }

            if (ch == '\"' || ch == '\'')
            {
                var startCh = ch;
                _pos++;
                while (_pos < _len)
                {
                    ch = _source[_pos++];
                    if (ch == startCh) break;
                    if (ch == '\\')
                    {
                        if (_pos < _len)
                        {
                            ch = _source[_pos++];
                            switch (ch)
                            {
                                case 't':
                                    _text.Append('\t');
                                    break;
                                case 'r':
                                    _text.Append('\r');
                                    break;
                                case 'n':
                                    _text.Append('\n');
                                    break;
                                default:
                                    _text.Append(ch);
                                    break;
                            }
                        }
                        else
                        {
                            _text.Append('\\');
                        }
                    }
                    else
                    {
                        _text.Append(ch);
                    }
                }

                ReadSpanSuffix();
                return startCh == '\"' ? OpCodeType.String : OpCodeType.Char;
            }

            if (IsDigit(ch) || ch == '-' || ch == '.')
            {
                if (ch == '-')
                {
                    _text.Append('-');
                    _pos++;
                }

                var gotDot = false;
                while (_pos < _len)
                {
                    ch = _source[_pos];
                    if (!gotDot && ch == '.')
                    {
                        _text.Append('.');
                        gotDot = true;
                        _pos++;
                    }
                    else if (IsDigit(ch))
                    {
                        _text.Append(ch);
                        _pos++;
                    }
                    else break;
                }

                ReadSpanSuffix();
                return OpCodeType.Number;
            }

            if (ch == '!' && _pos + 1 < _len)
            {
                if (_source[_pos + 1] == 'T')
                {
                    _text.Append(True);
                    _pos += 2;
                    ReadSpanSuffix();
                    return OpCodeType.Bool;
                }
                else if (_source[_pos + 1] == 'F')
                {
                    _text.Append(False);
                    _pos += 2;
                    ReadSpanSuffix();
                    return OpCodeType.Bool;
                }
            }

            if (ch == '(')
            {
                _text.Append('(');
                _span = new CodeSpan(_spanOffset, _spanOffset + 1);
                _pos++;
                return OpCodeType.Open;
            }
            if (ch == ')')
            {
                _text.Append(')');
                _span = new CodeSpan(_spanOffset, _spanOffset + 1);
                _pos++;
                return OpCodeType.Close;
            }
            if (ch == ',')
            {
                _text.Append(',');
                _span = new CodeSpan(_spanOffset, _spanOffset + 1);
                _pos++;
                return OpCodeType.Delim;
            }

            return OpCodeType.None;
        }

        private static bool IsAlpha(char ch) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
        private static bool IsDigit(char ch) => ch >= '0' && ch <= '9';

        private void ReadSpanSuffix()
        {
            if (_pos < _len && _source[_pos] == ':')
            {
                _pos++;
                var value = 0;
                char ch;
                while (_pos < _len && IsDigit(ch = _source[_pos]))
                {
                    value *= 10;
                    value += ch - '0';
                    _pos++;
                }

                if (_pos < _len && _source[_pos] == ':')
                {
                    _pos++;
                    var value2 = 0;
                    while (_pos < _len && IsDigit(ch = _source[_pos]))
                    {
                        value2 *= 10;
                        value2 += ch - '0';
                        _pos++;
                    }

                    _span = new CodeSpan(value, value + value2) + _spanOffset;
                }
                else
                {
                    var start = value / 100;
                    var end = start + value % 100;
                    _span = new CodeSpan(start, end) + _spanOffset;
                }
            }
            else
            {
                _span = CodeSpan.Empty + _spanOffset;
            }
        }

        public string ReadOpCode(bool throwOnError = true)
        {
            var resetPos = _pos;
            if (Read() != OpCodeType.OpCode)
            {
                if (throwOnError) throw new InvalidOpCodeSourceException("Expected op code.");
                _pos = resetPos;
                return null;
            }
            return _text.ToString();
        }

        public string ReadVariable(bool throwOnError = true)
        {
            var resetPos = _pos;
            if (Read() != OpCodeType.Variable)
            {
                if (throwOnError) throw new InvalidOpCodeSourceException("Expected variable.");
                _pos = resetPos;
                return null;
            }
            return _text.ToString();
        }

        public bool ReadOpen(bool throwOnError = true)
        {
            if (_pos < _len && _source[_pos] == '(')
            {
                _pos++;
                return true;
            }
            if (throwOnError) throw new InvalidOpCodeSourceException("Expected op code open token.");
            return false;
        }

        public bool ReadClose(bool throwOnError = true)
        {
            if (_pos < _len && _source[_pos] == ')')
            {
                _pos++;
                return true;
            }
            if (throwOnError) throw new InvalidOpCodeSourceException("Expected op code close token.");
            return false;
        }

        public bool ReadDelim(bool throwOnError = true)
        {
            if (_pos < _len && _source[_pos] == ',')
            {
                _pos++;
                return true;
            }
            if (throwOnError) throw new InvalidOpCodeSourceException("Expected op code delimiter.");
            return false;
        }
    }

    public enum OpCodeType
    {
        None,
        OpCode,
        Variable,
        Number,
        String,
        Bool,
        Char,
        Open,
        Close,
        Delim
    }
}
