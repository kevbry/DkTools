using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;

namespace DKX.Compilation.Objects
{
    static class ObjectAccess
    {
        public static CodeFragment GenerateMemberVariableGetter(CodeFragment thisFragment, uint varOffset, DataType varDataType, Span span)
        {
            switch (varDataType.BaseType)
            {
                case BaseType.Bool:
                    return new CodeFragment($"dkx_getint4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Short:
                    return new CodeFragment($"dkx_getint2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.UShort:
                    return new CodeFragment($"dkx_getuns2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Int:
                    return new CodeFragment($"dkx_getint4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.UInt:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Numeric:
                    return new CodeFragment($"dkx_getint{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.UNumeric:
                    return new CodeFragment($"dkx_getuns{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Char:
                    return new CodeFragment($"dkx_getint2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.String:
                    return new CodeFragment($"dkx_getstr({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Date:
                    return new CodeFragment($"dkx_getdate({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Time:
                    return new CodeFragment($"dkx_gettime({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Enum:
                    return new CodeFragment($"dkx_getuns2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Class:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                default:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
            }
        }

        public static CodeFragment GenerateMemberVariableSetter(CodeFragment thisFragment, uint varOffset, DataType varDataType, Span span, CodeFragment valueFragment)
        {
            switch (varDataType.BaseType)
            {
                case BaseType.Bool:
                    return new CodeFragment($"dkx_setint4({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Short:
                    return new CodeFragment($"dkx_setint2({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.UShort:
                    return new CodeFragment($"dkx_setuns2({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Int:
                    return new CodeFragment($"dkx_setint4({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.UInt:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Numeric:
                    return new CodeFragment($"dkx_setint{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.UNumeric:
                    return new CodeFragment($"dkx_setuns{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Char:
                    return new CodeFragment($"dkx_setint2({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.String:
                    return new CodeFragment($"dkx_setstr({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Date:
                    return new CodeFragment($"dkx_setdate({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Time:
                    return new CodeFragment($"dkx_settime({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Enum:
                    return new CodeFragment($"dkx_setuns2({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                case BaseType.Class:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
                default:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, Expressions.OpPrec.None, span, readOnly: true);
            }
        }
    }
}
