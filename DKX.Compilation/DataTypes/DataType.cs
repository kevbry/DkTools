using DK;
using DK.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DKX.Compilation.DataTypes
{
    public struct DataType
    {
        private BaseType _baseType;
        private byte _width;
        private byte _scale;
        private string _options;

        public const byte MinNumericWidth = 1;
        public const byte MaxNumericWidth = 38;
        public const byte MinNumericScale = 0;
        public const byte MaxNumericScale = 38;
        public const byte MinStringLength = 1;
        public const byte MaxStringLength = 255;

        // General limits for width and scale
        public const byte MinWidth = 1;
        public const byte MaxWidth = 255;
        public const byte MinScale = 0;
        public const byte MaxScale = 38;

        public DataType(BaseType baseType)
        {
            _baseType = baseType;
            _width = 0;
            _scale = 0;
            _options = null;
        }

        public DataType(BaseType baseType, byte width = 0, byte scale = 0)
        {
            switch (baseType)
            {
                case BaseType.Numeric:
                case BaseType.UNumeric:
                    if (width < MinNumericWidth || width > MaxNumericWidth) throw new ArgumentOutOfRangeException(nameof(width));
                    if (scale < MinNumericScale || scale > MaxNumericScale) throw new ArgumentOutOfRangeException(nameof(scale));
                    break;
                case BaseType.String:
                    if (width < MinStringLength || width > MaxStringLength) throw new ArgumentOutOfRangeException(nameof(width));
                    if (scale != 0) throw new ArgumentOutOfRangeException(nameof(scale));
                    break;
                default:
                    if (width != 0) throw new ArgumentOutOfRangeException(nameof(width));
                    if (scale != 0) throw new ArgumentOutOfRangeException(nameof(scale));
                    break;
            }

            _baseType = baseType;
            _width = width;
            _scale = scale;
            _options = null;
        }

        public DataType(BaseType baseType, string[] options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            switch (baseType)
            {
                case BaseType.Enum:
                    if (options.Length == 0) throw new ArgumentException("Options list must contain at least 1 value.");
                    break;
                case BaseType.Like1:
                    if (options.Length != 1) throw new ArgumentException("Options list must contain exactly 1 value.");
                    break;
                case BaseType.Like2:
                    if (options.Length != 2) throw new ArgumentException("Options list must contain exactly 2 values.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baseType));
            }

            _baseType = baseType;
            _width = 0;
            _scale = 0;
            _options = StringListToOptions(options ?? throw new ArgumentNullException(nameof(options)));
        }

        private DataType(BaseType baseType, byte width, byte scale, string[] options)
        {
            _baseType = baseType;
            _width = width;
            _scale = scale;
            _options = options != null ? StringListToOptions(options) : null;
        }

        public BaseType BaseType => _baseType;
        public short Width => _width;
        public short Scale => _scale;
        public string[] Options => OptionsToStringList(_options ?? string.Empty);
        public bool IsVoid => _baseType == BaseType.Void;

        public override string ToString() => ToCode();

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(DataType)) return false;
            var dt = (DataType)obj;
            return dt._baseType == _baseType && dt._width == _width && dt._scale == _scale && dt._options == _options;
        }

        public override int GetHashCode() => _baseType.GetHashCode() + _width.GetHashCode() * 13 + _scale.GetHashCode() * 23 + (_options?.GetHashCode() ?? 0) * 31;

        public static bool operator ==(DataType a, DataType b) => a._baseType == b._baseType && a._width == b._width && a._scale == b._scale && a._options == b._options;
        public static bool operator !=(DataType a, DataType b) => a._baseType != b._baseType || a._width != b._width || a._scale != b._scale || a._options != b._options;

        #region Predefined Data Types
        public static readonly DataType Bool = new DataType(BaseType.Bool);
        public static readonly DataType Char = new DataType(BaseType.Char);
        public static readonly DataType Date = new DataType(BaseType.Date);
        public static readonly DataType Indrel = new DataType(BaseType.Indrel);
        public static readonly DataType Int = new DataType(BaseType.Int);
        public static readonly DataType Short = new DataType(BaseType.Short);
        public static readonly DataType String255 = new DataType(BaseType.String, width: 255);
        public static readonly DataType Table = new DataType(BaseType.Table);
        public static readonly DataType Time = new DataType(BaseType.Time);
        public static readonly DataType UChar = new DataType(BaseType.UChar);
        public static readonly DataType UInt = new DataType(BaseType.UInt);
        public static readonly DataType Unsupported = new DataType(BaseType.Unsupported);
        public static readonly DataType UShort = new DataType(BaseType.UShort);
        public static readonly DataType Variant = new DataType(BaseType.Variant);
        public static readonly DataType Void = new DataType(BaseType.Void);
        #endregion

        #region Code Generation
        public string ToCode()
        {
            switch (_baseType)
            {
                case BaseType.Unsupported:
                    return "unsupported";
                case BaseType.Void:
                    return "void";
                case BaseType.Bool:
                    return "bool";
                case BaseType.Short:
                    return "short";
                case BaseType.UShort:
                    return "ushort";
                case BaseType.Int:
                    return "int";
                case BaseType.UInt:
                    return "uint";
                case BaseType.Numeric:
                    if (_scale != 0) return $"numeric({_width}, {_scale})";
                    return $"numeric({_width})";
                case BaseType.UNumeric:
                    if (_scale != 0) return $"unsigned({_width}, {_scale})";
                    return $"unsigned({_width})";
                case BaseType.Char:
                    return "char";
                case BaseType.UChar:
                    return "uchar";
                case BaseType.String:
                    if (_width == MaxStringLength) return "string";
                    return $"string({_width})";
                case BaseType.Date:
                    return "date";
                case BaseType.Time:
                    return "time";
                case BaseType.Enum:
                    var sb = new StringBuilder();
                    sb.Append("enum { ");
                    var first = true;
                    foreach (var option in OptionsToStringList(_options))
                    {
                        if (first) first = false;
                        else sb.Append(", ");
                        sb.Append(NormalizeEnumOption(option));
                    }
                    sb.Append(" }");
                    return sb.ToString();
                case BaseType.Table:
                    return "table";
                case BaseType.Indrel:
                    return "indrel";
                case BaseType.Variant:
                    return "variant";
                case BaseType.Like1:
                    return $"like {_options[0]}";
                case BaseType.Like2:
                    return $"like {_options[0]} {_options[1]}";
                default:
                    throw new InvalidBaseTypeException(_baseType);
            }
        }

        private string NormalizeEnumOption(string option)
        {
            if (string.IsNullOrEmpty(option)) return "\" \"";
            if (option.IsWord()) return option;
            return CodeParser.StringToStringLiteral(option);
        }
        #endregion

        #region Code Parsing
        public static DataType? Parse(CodeParser code)
        {
            var startPos = code.Position;

            if (code.ReadWord())
            {
                var dataType = ParseWord(code, code.Text);
                if (dataType == null)
                {
                    code.Position = startPos;
                    return null;
                }

                return dataType;
            }

            return null;
        }

        private static DataType? ParseWord(CodeParser code, string word)
        {
            switch (word)
            {
                case "unsupported":
                    return Unsupported;
                case "void":
                    return Void;
                case "bool":
                    return Bool;
                case "short":
                    return Short;
                case "ushort":
                    return UShort;
                case "int":
                    return Int;
                case "uint":
                    return UInt;
                case "numeric":
                    if (!code.ReadExact('(')) return null;
                    if (!code.ReadNumber() || !byte.TryParse(code.Text, out var width) || width < MinNumericWidth || width > MaxNumericWidth) return null;
                    byte scale = 0;
                    if (code.ReadExact(','))
                    {
                        if (!code.ReadNumber() || !byte.TryParse(code.Text, out scale) || scale < MinNumericScale || scale > MaxNumericScale) return null;
                    }
                    if (!code.ReadExact(')')) return null;
                    return new DataType(BaseType.Numeric, width: width, scale: scale);
                case "unsigned":
                    if (!code.ReadExact('(')) return null;
                    if (!code.ReadNumber() || !byte.TryParse(code.Text, out width) || width < MinNumericWidth || width > MaxNumericWidth) return null;
                    scale = 0;
                    if (code.ReadExact(','))
                    {
                        if (!code.ReadNumber() || !byte.TryParse(code.Text, out scale) || scale < MinNumericScale || scale > MaxNumericScale) return null;
                    }
                    if (!code.ReadExact(')')) return null;
                    return new DataType(BaseType.UNumeric, width: width, scale: scale);
                case "char":
                    return Char;
                case "uchar":
                    return UChar;
                case "string":
                    if (code.ReadExact('('))
                    {
                        if (!code.ReadNumber() || !byte.TryParse(code.Text, out width) || width < MinStringLength || width > MaxStringLength) return null;
                        if (!code.ReadExact(')')) return null;
                        return new DataType(BaseType.String, width: width);
                    }
                    return String255;
                case "date":
                    return Date;
                case "time":
                    return Time;
                case "enum":
                    if (!code.ReadExact('{')) return null;
                    var options = new List<string>();
                    while (true)
                    {
                        if (code.ReadWord()) options.Add(code.Text);
                        else if (code.ReadStringLiteral()) options.Add(CodeParser.StringLiteralToString(code.Text));
                        else return null;

                        if (code.ReadExact('}')) return new DataType(BaseType.Enum, options.ToArray());
                        if (code.ReadExact(',')) continue;
                        return null;
                    }
                case "table":
                    return Table;
                case "indrel":
                    return Indrel;
                case "variant":
                    return Variant;
                case "like":
                    if (!code.ReadWord()) return null;
                    var word1 = code.Text;
                    if (code.ReadExact('.'))
                    {
                        if (!code.ReadWord()) return null;
                        return new DataType(BaseType.Like2, new string[] { word1, code.Text });
                    }
                    return new DataType(BaseType.Like1, new string[] { word1 });
            }

            return null;
        }
        #endregion

        #region Options
        private static string StringListToOptions(IEnumerable<string> optionsList) => string.Join("|", optionsList.Select(x => EncodeOption(x)));

        private static string[] OptionsToStringList(string options) => options.Split('|').Select(x => DecodeOption(x)).ToArray();

        private static readonly char[] _optionsEscapeChars = new char[] { '\\', '|' };

        private static string EncodeOption(string optionText)
        {
            if (optionText.IndexOfAny(_optionsEscapeChars) < 0) return optionText;

            var sb = new StringBuilder(optionText.Length + optionText.Length >> 1);

            foreach (var ch in optionText)
            {
                if (ch == '\\') sb.Append("\\\\");
                else if (ch == '|') sb.Append("\\!");
                else sb.Append(ch);
            }

            return sb.ToString();
        }

        private static string DecodeOption(string encodedText)
        {
            if (encodedText.IndexOf('\\') < 0) return encodedText;

            var sb = new StringBuilder(encodedText.Length);

            var afterEscape = false;
            foreach (var ch in encodedText)
            {
                if (afterEscape)
                {
                    afterEscape = false;
                    if (ch == '\\') sb.Append('\\');
                    else if (ch == '!') sb.Append('|');
                    else throw new ArgumentException($"Invalid escape sequence '\\{ch}' in encoded option string.");
                }
                else if (ch == '\\')
                {
                    afterEscape = true;
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }
        #endregion
    }

    class InvalidDataTypeDeserializationException : Exception
    {
        public InvalidDataTypeDeserializationException(string serializedText) : base($"Unable to deserialize a data type from '{serializedText}'.") { }
    }
}
