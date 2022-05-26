using DK;
using DK.Code;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Resolving;
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
        private IClass _class;

        // General limits for width and scale
        public const byte MinWidth = 1;
        public const byte MaxWidth = 255;
        public const byte MinScale = 0;
        public const byte MaxScale = 38;

        public static readonly DataType[] EmptyArray = new DataType[0];

        public DataType(BaseType baseType)
        {
            _baseType = baseType;
            _width = 0;
            _scale = 0;
            _options = null;
            _class = null;
        }

        public DataType(BaseType baseType, byte width = 0, byte scale = 0)
        {
            switch (baseType)
            {
                case BaseType.Numeric:
                case BaseType.UNumeric:
                    if (width < DkxConst.Numeric.MinWidth || width > DkxConst.Numeric.MaxWidth) throw new ArgumentOutOfRangeException(nameof(width));
                    if (scale < DkxConst.Numeric.MinScale || scale > DkxConst.Numeric.MaxScale) throw new ArgumentOutOfRangeException(nameof(scale));
                    break;
                case BaseType.String:
                    if (width < DkxConst.String.MinLength || width > DkxConst.String.MaxLength) throw new ArgumentOutOfRangeException(nameof(width));
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
            _class = null;
        }

        public DataType(BaseType baseType, string[] options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            switch (baseType)
            {
                case BaseType.Enum:
                    if (options.Length == 0) throw new ArgumentException("Options list must contain at least 1 value.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baseType));
            }

            _baseType = baseType;
            _width = 0;
            _scale = 0;
            _options = StringListToOptions(options ?? throw new ArgumentNullException(nameof(options)));
            _class = null;
        }

        public DataType(IClass class_)
        {
            _baseType = BaseType.Class;
            _width = 0;
            _scale = 0;
            _options = null;
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));
        }

        public BaseType BaseType => _baseType;
        public IClass Class => _class;
        public short Width => _width;
        public short Scale => _scale;
        public string[] Options => OptionsToStringList(_options ?? string.Empty);
        public bool IsUnsupported => _baseType == BaseType.Unsupported;
        public bool IsSigned => _baseType.IsSigned();
        public bool IsValue => _baseType != BaseType.Void && _baseType != BaseType.Unsupported;
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
                case BaseType.String:
                    if (_width == DkxConst.String.MaxLength) return "string";
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
                case BaseType.Class:
                    return _class.FullClassName;
                default:
                    throw new InvalidBaseTypeException(_baseType);
            }
        }

        public string ToWbdkCode()
        {
            switch (_baseType)
            {
                case BaseType.Unsupported:
                    return "int";
                case BaseType.Void:
                    return "void";
                case BaseType.Bool:
                    return "int";
                case BaseType.Short:
                    return "short";
                case BaseType.UShort:
                    return "unsigned short";
                case BaseType.Int:
                    return "int";
                case BaseType.UInt:
                    return "unsigned int";
                case BaseType.Numeric:
                    if (_scale != 0) return $"numeric({_width}, {_scale})";
                    return $"numeric({_width})";
                case BaseType.UNumeric:
                    if (_scale != 0) return $"numeric({_width}, {_scale}) unsigned";
                    return $"numeric({_width}) unsigned";
                case BaseType.Char:
                    return "char";
                case BaseType.String:
                    if (_width == DkxConst.String.MaxLength) return "char(255)";
                    return $"char({_width})";
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
                case BaseType.Class:
                    return "unsigned int";
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
        public static bool TryParse(string code, out DataType dataTypeOut) => TryParse(new CodeParser(code), out dataTypeOut, out _);

        public static bool TryParse(CodeParser code, out DataType dataTypeOut) => TryParse(code, out dataTypeOut, out _);

        public static bool TryParse(CodeParser code, out DataType dataTypeOut, out CodeSpan spanOut)
        {
            var startPos = code.Position;

            if (code.ReadWord())
            {
                if (!TryParseWord(code, code.Text, code.Span, out dataTypeOut, out spanOut))
                {
                    code.Position = startPos;
                    return false;
                }

                return true;
            }

            dataTypeOut = default;
            spanOut = default;
            return false;
        }

        private static bool TryParseWord(CodeParser code, string word, CodeSpan wordSpan, out DataType dataTypeOut, out CodeSpan spanOut)
        {
            switch (word)
            {
                case "unsupported":
                    dataTypeOut = Unsupported;
                    spanOut = wordSpan;
                    return true;

                case "void":
                    dataTypeOut = Void;
                    spanOut = wordSpan;
                    return true;

                case "bool":
                    dataTypeOut = Bool;
                    spanOut = wordSpan;
                    return true;

                case "short":
                    dataTypeOut = Short;
                    spanOut = wordSpan;
                    return true;

                case "ushort":
                    dataTypeOut = UShort;
                    spanOut = wordSpan;
                    return true;

                case "int":
                    dataTypeOut = Int;
                    spanOut = wordSpan;
                    return true;

                case "uint":
                    dataTypeOut = UInt;
                    spanOut = wordSpan;
                    return true;

                case "numeric":
                    if (!code.ReadExact('('))
                    {
                        dataTypeOut = default;
                        spanOut = default;
                        return false;
                    }
                    if (!code.ReadNumber() || !byte.TryParse(code.Text, out var width) || width < DkxConst.Numeric.MinWidth || width > DkxConst.Numeric.MaxWidth)
                    {
                        dataTypeOut = default;
                        spanOut = default;
                        return false;
                    }
                    byte scale = 0;
                    if (code.ReadExact(','))
                    {
                        if (!code.ReadNumber() || !byte.TryParse(code.Text, out scale) || scale < DkxConst.Numeric.MinScale || scale > DkxConst.Numeric.MaxScale)
                        {
                            dataTypeOut = default;
                            spanOut = default;
                            return false;
                        }
                    }
                    if (!code.ReadExact(')'))
                    {
                        dataTypeOut = default;
                        spanOut = default;
                        return false;
                    }
                    dataTypeOut = new DataType(BaseType.Numeric, width: width, scale: scale);
                    spanOut = wordSpan.Envelope(code.Span);
                    return true;

                case "unsigned":
                    if (!code.ReadExact('('))
                    {
                        dataTypeOut = default;
                        spanOut = default;
                        return false;
                    }
                    if (!code.ReadNumber() || !byte.TryParse(code.Text, out width) || width < DkxConst.Numeric.MinWidth || width > DkxConst.Numeric.MaxWidth)
                    {
                        dataTypeOut = default;
                        spanOut = default;
                        return false;
                    }
                    scale = 0;
                    if (code.ReadExact(','))
                    {
                        if (!code.ReadNumber() || !byte.TryParse(code.Text, out scale) || scale < DkxConst.Numeric.MinScale || scale > DkxConst.Numeric.MaxScale)
                        {
                            dataTypeOut = default;
                            spanOut = default;
                            return false;
                        }
                    }
                    if (!code.ReadExact(')'))
                    {
                        dataTypeOut = default;
                        spanOut = default;
                        return false;
                    }
                    dataTypeOut = new DataType(BaseType.UNumeric, width: width, scale: scale);
                    spanOut = wordSpan.Envelope(code.Span);
                    return true;

                case "char":
                    dataTypeOut = Char;
                    spanOut = wordSpan;
                    return true;

                case "string":
                    if (code.ReadExact('('))
                    {
                        if (!code.ReadNumber() || !byte.TryParse(code.Text, out width) || width < DkxConst.String.MinLength || width > DkxConst.String.MaxLength)
                        {
                            dataTypeOut = default;
                            spanOut = default;
                            return false;
                        }
                        if (!code.ReadExact(')'))
                        {
                            dataTypeOut = default;
                            spanOut = default;
                            return false;
                        }
                        dataTypeOut = new DataType(BaseType.String, width: width);
                        spanOut = wordSpan.Envelope(code.Span);
                        return true;
                    }
                    dataTypeOut = String255;
                    spanOut = wordSpan;
                    return true;

                case "date":
                    dataTypeOut = Date;
                    spanOut = wordSpan;
                    return true;

                case "time":
                    dataTypeOut = Time;
                    spanOut = wordSpan;
                    return true;

                case "enum":
                    if (!code.ReadExact('{'))
                    {
                        dataTypeOut = default;
                        spanOut = default;
                        return false;
                    }
                    var options = new List<string>();
                    while (true)
                    {
                        if (code.ReadWord()) options.Add(code.Text);
                        else if (code.ReadStringLiteral()) options.Add(CodeParser.StringLiteralToString(code.Text));
                        else
                        {
                            dataTypeOut = default;
                            spanOut = default;
                            return false;
                        }

                        if (code.ReadExact('}'))
                        {
                            dataTypeOut = new DataType(BaseType.Enum, options.ToArray());
                            spanOut = wordSpan.Envelope(code.Span);
                            return true;
                        }

                        if (code.ReadExact(',')) continue;

                        dataTypeOut = default;
                        spanOut = default;
                        return false;
                    }

                case "table":
                    dataTypeOut = Table;
                    spanOut = wordSpan;
                    return true;

                case "indrel":
                    dataTypeOut = Indrel;
                    spanOut = wordSpan;
                    return true;

                case "variant":
                    dataTypeOut = Variant;
                    spanOut = wordSpan;
                    return true;
            }

            dataTypeOut = default;
            spanOut = default;
            return false;
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

        #region Capabilities
        public uint DataSize
        {
            get
            {
                switch (_baseType)
                {
                    case BaseType.Short:
                    case BaseType.UShort:
                    case BaseType.Char:
                    case BaseType.Date:
                    case BaseType.Time:
                    case BaseType.Enum:
                        return 2;
                    case BaseType.Bool:
                    case BaseType.Int:
                    case BaseType.UInt:
                        return 4;
                    case BaseType.Numeric:
                    case BaseType.UNumeric:
                        if (_width <= 2) return 1;
                        if (_width <= 4) return 2;
                        if (_width <= 9) return 4;
                        if (_width <= 14) return 6;
                        if (_width <= 18) return 8;
                        return 9;
                    case BaseType.String:
                        return (uint)(_width + 1) * 2;
                    case BaseType.Class:
                        return 4;
                    default:
                        return 0;
                }
            }
        }

        public int NumBits
        {
            get
            {
                switch (_baseType)
                {
                    case BaseType.Char:
                    case BaseType.Short:
                    case BaseType.UShort:
                    case BaseType.Date:
                    case BaseType.Time:
                    case BaseType.Enum:
                        return 16;
                    case BaseType.Bool:
                    case BaseType.Int:
                    case BaseType.UInt:
                        return 32;
                    case BaseType.Numeric:
                    case BaseType.UNumeric:
                        if (_width <= 2) return 8;
                        if (_width <= 4) return 16;
                        if (_width <= 9) return 32;
                        if (_width < 14) return 48;
                        if (_width < 18) return 64;
                        return 72;
                    default:
                        return 0;
                }
            }
        }

        public decimal MinNumber
        {
            get
            {
                switch (_baseType)
                {
                    case BaseType.Bool:
                    case BaseType.UShort:
                    case BaseType.UInt:
                    case BaseType.UNumeric:
                    case BaseType.Char:
                    case BaseType.Date:
                    case BaseType.Time:
                    case BaseType.Enum:
                        return 0;
                    case BaseType.Short:
                        return short.MaxValue;
                    case BaseType.Int:
                        return int.MaxValue;
                    case BaseType.Numeric:
                        return -(decimal)Math.Pow(10, _width);
                    default:
                        return 0;
                }
            }
        }

        public decimal MaxNumber
        {
            get
            {
                switch (_baseType)
                {
                    case BaseType.Bool:
                        return 1;
                    case BaseType.UShort:
                    case BaseType.Char:
                    case BaseType.Date:
                    case BaseType.Time:
                    case BaseType.Enum:
                        return ushort.MaxValue;
                    case BaseType.UInt:
                        return uint.MaxValue;
                    case BaseType.Short:
                        return short.MaxValue;
                    case BaseType.Int:
                        return int.MaxValue;
                    case BaseType.Numeric:
                    case BaseType.UNumeric:
                        return (decimal)Math.Pow(10, _width);
                    default:
                        throw new InvalidBaseTypeException(_baseType);
                }
            }
        }

        public bool IsSuitableForVariable
        {
            get
            {
                switch (_baseType)
                {
                    case BaseType.Bool:
                    case BaseType.Short:
                    case BaseType.UShort:
                    case BaseType.Int:
                    case BaseType.UInt:
                    case BaseType.Numeric:
                    case BaseType.UNumeric:
                    case BaseType.Char:
                    case BaseType.String:
                    case BaseType.Date:
                    case BaseType.Time:
                    case BaseType.Enum:
                    case BaseType.Variant:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsSuitableForIncDec
        {
            get
            {
                switch (_baseType)
                {
                    case BaseType.Char:
                    case BaseType.Short:
                    case BaseType.UShort:
                    case BaseType.Int:
                    case BaseType.UInt:
                    case BaseType.Numeric:
                    case BaseType.UNumeric:
                    case BaseType.Date:
                    case BaseType.Time:
                    case BaseType.Enum:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsSuitableForNumericMath
        {
            get
            {
                switch (_baseType)
                {
                    case BaseType.Char:
                    case BaseType.Short:
                    case BaseType.UShort:
                    case BaseType.Int:
                    case BaseType.UInt:
                    case BaseType.Numeric:
                    case BaseType.UNumeric:
                    case BaseType.Date:
                    case BaseType.Time:
                    case BaseType.Enum:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsSuitableForNew
        {
            get
            {
                switch (_baseType)
                {
                    case BaseType.Class:
                        return true;
                    default:
                        return false;
                }
            }
        }
        #endregion
    }

    class InvalidDataTypeDeserializationException : Exception
    {
        public InvalidDataTypeDeserializationException(string serializedText) : base($"Unable to deserialize a data type from '{serializedText}'.") { }
    }
}
