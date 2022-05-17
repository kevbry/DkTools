using DK;
using DK.Code;
using System.Collections.Generic;

namespace DKX.Compilation.DataTypes
{
    public static class DkDataTypeParser
    {
        public static DataType? Parse(CodeParser code)
        {
            var startPos = code.Position;

            if (!code.ReadWord()) return null;
            var dataType = ProcessWord(code, code.Text);

            if (dataType == null)
            {
                code.Position = startPos;
                return null;
            }

            return dataType;
        }

        private static DataType? ProcessWord(CodeParser code, string word)
        {
            #region void
            if (word.EqualsI("void"))
            {
                return DataType.Void;
            }
            #endregion
            #region numeric
            if (word.EqualsI("numeric") || word.EqualsI("decimal"))
            {
                if (!code.ReadExact('(')) return null;
                if (!code.ReadNumber()) return null;
                if (!byte.TryParse(code.Text, out var width) || width < DataType.MinNumericWidth || width > DataType.MaxNumericWidth) return null;
                byte scale = 0;
                if (code.ReadExact(','))
                {
                    if (!code.ReadNumber()) return null;
                    if (!byte.TryParse(code.Text, out scale) || scale < DataType.MinNumericScale || scale > DataType.MaxNumericScale) return null;
                    if (!code.ReadExact(')')) return null;
                }
                else
                {
                    scale = 0;
                    if (!code.ReadExact(')')) return null;
                }

                var unsigned = false;
                var currency = false;
                var mask = false;
                while (!code.EndOfFile)
                {
                    if (!unsigned && code.ReadExactWholeWordI("unsigned")) unsigned = true;
                    else if (!currency && code.ReadExactWholeWordI("currency")) currency = true;
                    else if (!currency && code.ReadExactWholeWordI("local_currency")) currency = true;
                    else if (!mask && code.ReadIncludeStringLiteral()) mask = true;
                    else if (ReadAttribute(code)) { }
                    else break;
                }

                return new DataType(unsigned ? BaseType.UNumeric : BaseType.Numeric, width: width, scale: scale);
            }
            #endregion
            #region signed
            if (word.EqualsI("signed"))
            {
                return DataType.Int;
            }
            #endregion
            #region unsigned
            if (word.EqualsI("unsigned"))
            {
                if (code.ReadNumber())
                {
                    if (!byte.TryParse(code.Text, out var width) || width < DataType.MinNumericWidth || width >= DataType.MaxNumericWidth) return null;
                    return new DataType(BaseType.UNumeric, width: width);
                }

                var baseType = BaseType.UInt;
                var mask = false;
                while (!code.EndOfFile)
                {
                    if (code.ReadExactWholeWordI("int")) baseType = BaseType.UInt;
                    else if (code.ReadExactWholeWordI("long")) baseType = BaseType.UInt;
                    else if (code.ReadExactWholeWordI("short")) baseType = BaseType.UShort;
                    else if (code.ReadExactWholeWordI("char")) baseType = BaseType.UChar;
                    else if (!mask && code.ReadIncludeStringLiteral()) mask = true;
                    else if (ReadAttribute(code)) { }
                    else break;
                }

                return new DataType(baseType);
            }
            #endregion
            #region int
            if (word.EqualsI("int"))
            {
                if (code.ReadNumber())
                {
                    if (!byte.TryParse(code.Text, out var width) || width < DataType.MinNumericWidth || width > DataType.MaxNumericWidth) return null;

                    var mask = false;
                    while (!code.EndOfFile)
                    {
                        if (!mask && code.ReadStringLiteral()) mask = true;
                        else if (ReadAttribute(code)) { }
                        else break;
                    }

                    return new DataType(BaseType.Numeric, width: width);
                }
                else
                {
                    var mask = false;
                    var unsigned = false;
                    while (!code.EndOfFile)
                    {
                        if (code.ReadExactWholeWordI("unsigned")) unsigned = true;
                        else if (code.ReadExactWholeWordI("signed")) unsigned = false;
                        else if (!mask && code.ReadStringLiteral()) mask = true;
                        else if (ReadAttribute(code)) { }
                        else break;
                    }

                    return unsigned ? DataType.UInt : DataType.Int;
                }
            }
            #endregion
            #region short
            if (word.EqualsI("short"))
            {
                var mask = false;
                var unsigned = false;
                while (!code.EndOfFile)
                {
                    if (code.ReadExactWholeWordI("unsigned")) unsigned = true;
                    if (code.ReadExactWholeWordI("signed")) unsigned = false;
                    else if (!mask && code.ReadStringLiteral()) mask = true;
                    else if (ReadAttribute(code)) { }
                    else break;
                }

                return unsigned ? DataType.UShort : DataType.Short;
            }
            #endregion
            #region long
            if (word.EqualsI("long"))
            {
                var mask = false;
                var unsigned = false;
                while (!code.EndOfFile)
                {
                    if (code.ReadExactWholeWordI("unsigned")) unsigned = true;
                    if (code.ReadExactWholeWordI("signed")) unsigned = false;
                    else if (!mask && code.ReadStringLiteral()) mask = true;
                    else if (ReadAttribute(code)) { }
                    else break;
                }

                return unsigned ? DataType.UInt : DataType.Int;
            }
            #endregion
            #region char
            if (word.EqualsI("char") || word.EqualsI("character") || word.EqualsI("varchar"))
            {
                if (code.ReadExact('('))
                {
                    if (!code.ReadNumber()) return null;
                    if (!byte.TryParse(code.Text, out var width) || width < 0 || width > DataType.MaxStringLength) return null;
                    if (!code.ReadExact(')')) return null;

                    var mask = false;
                    while (!code.EndOfFile)
                    {
                        if (!mask && code.ReadStringLiteral()) mask = true;
                        else if (ReadAttribute(code)) { }
                        else break;
                    }

                    return new DataType(BaseType.String, width: width);
                }
                else
                {
                    return DataType.Char;
                }
            }
            #endregion
            #region string
            if (word.EqualsI("string"))
            {
                if (!code.ReadNumber()) return null;
                if (!byte.TryParse(code.Text, out var width) || width < 0 || width > DataType.MaxStringLength) return null;

                var mask = false;
                while (!code.EndOfFile)
                {
                    if (!mask && code.ReadStringLiteral()) mask = true;
                    else if (ReadAttribute(code)) { }
                    else break;
                }

                return new DataType(BaseType.String, width: width);
            }
            #endregion
            #region date
            if (word.EqualsI("date"))
            {
                var mask = false;
                while (!code.EndOfFile)
                {
                    if (!mask && code.ReadStringLiteral()) mask = true;
                    else if (ReadAttribute(code, "shortform", "longform", "alternate")) { }
                    else break;
                }

                return DataType.Date;
            }
            #endregion
            #region time
            if (word.EqualsI("time"))
            {
                var resetPos = code.Position;
                if (code.ReadNumber())
                {
                    if (!int.TryParse(code.Text, out var width) || width < 4 || width >= 12) code.Position = resetPos;
                }

                return DataType.Time;
            }
            #endregion
            #region enum
            if (word.EqualsI("enum"))
            {
                // Read tokens before the option list
                var width = -1;
                while (!code.EndOfFile)
                {
                    if (width == -1 && code.ReadNumber())
                    {
                        if (!int.TryParse(code.Text, out width) || width < 1 || width > DataType.MaxStringLength) return null;
                    }
                    else if (ReadAttribute(code, "alterable", "required", "nowarn", "numeric")) { }
                    else break;
                }

                // Read the option list
                if (!code.ReadExact('{')) return null;
                var options = new List<string>();
                while (!code.EndOfFile)
                {
                    if (code.ReadExact('}')) break;
                    if (code.ReadExact(',')) continue;
                    if (code.ReadWord())
                    {
                        var option = code.Text;
                        if (options.Contains(option)) return null;
                        options.Add(code.Text);
                    }
                    else if (code.ReadStringLiteral())
                    {
                        var option = CodeParser.StringLiteralToString(code.Text);
                        if (options.Contains(option)) return null;
                        options.Add(option);
                    }
                    else return null;
                }

                while (ReadAttribute(code)) ;

                return new DataType(BaseType.Enum, options.ToArray());
            }
            #endregion
            #region table
            if (word.EqualsI("table"))
            {
                return DataType.Table;
            }
            #endregion
            #region indrel
            if (word.EqualsI("indrel"))
            {
                return DataType.Indrel;
            }
            #endregion
            #region command
            if (word.EqualsI("command"))
            {
                return DataType.Unsupported;
            }
            #endregion
            #region Section
            if (word.EqualsI("Section"))
            {
                if (code.ReadExact("Level"))
                {
                    code.ReadNumber();
                }

                while (ReadAttribute(code)) ;

                return DataType.Unsupported;
            }
            #endregion
            #region scroll
            if (word.EqualsI("scroll"))
            {
                code.ReadNumber();

                while (ReadAttribute(code)) ;

                return DataType.Unsupported;
            }
            #endregion
            #region graphic
            if (word.EqualsI("graphic"))
            {
                code.ReadNumber();	// rows
                code.ReadNumber();	// columns
                code.ReadNumber();	// bytes
                while (ReadAttribute(code)) ;
                return DataType.Unsupported;
            }
            #endregion
            #region interface
            if (word.EqualsI("interface"))
            {
                code.ReadWord();
                return DataType.Unsupported;
            }
            #endregion
            #region variant
            if (word.EqualsI("variant"))
            {
                return DataType.Variant;
            }
            #endregion
            #region oleobject
            if (word.EqualsI("oleobject"))
            {
                return DataType.Unsupported;
            }
            #endregion
            #region Boolean_t
            if (word.EqualsI("Boolean_t"))
            {
                return DataType.Bool;
            }
            #endregion

            return null;
        }

        private static bool ReadAttribute(CodeParser code, params string[] extraTokens)
        {
            var startPos = code.Position;
            if (code.ReadWord())
            {
                var word = code.Text;
                switch (word)
                {
                    case "ALLCAPS":
                    case "AUTOCAPS":
                    case "LEADINGZEROS":
                    case "NOCHANGE":
                    case "NODISPLAY":
                    case "NOECHO":
                    case "NOINPUT":
                    case "NOPICK":
                    case "NOUSE":
                    case "REQUIRED":
                    case "PROBE":
                        return true;

                    default:
                        code.Position = startPos;
                        return false;
                }
            }
            else if (code.ReadExact("@neutral"))
            {
                return true;
            }

            return false;
        }
    }
}
