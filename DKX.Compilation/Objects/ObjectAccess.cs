using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;

namespace DKX.Compilation.Objects
{
    static class ObjectAccess
    {
        public static CodeFragment GenerateMemberVariableGetter(CodeFragment thisFragment, uint varOffset, DataType varDataType, Span span)
        {
            switch (varDataType.BaseType)
            {
                case BaseType.Bool:
                    return new CodeFragment($"dkx_getint4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.Short:
                    return new CodeFragment($"dkx_getint2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.UShort:
                    return new CodeFragment($"dkx_getuns2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.Int:
                    return new CodeFragment($"dkx_getint4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.UInt:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.Numeric:
                    return new CodeFragment($"dkx_getint{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.UNumeric:
                    return new CodeFragment($"dkx_getuns{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.Char:
                    return new CodeFragment($"dkx_getint2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.String:
                    return new CodeFragment($"dkx_getstr({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.Date:
                    return new CodeFragment($"dkx_getdate({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.Time:
                    return new CodeFragment($"dkx_gettime({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.Enum:
                    return new CodeFragment($"dkx_getuns2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                case BaseType.Class:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
                default:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span);
            }
        }

        public static CodeFragment GenerateMemberVariableSetter(CodeFragment thisFragment, uint varOffset, DataType varDataType, Span span, CodeFragment valueFragment)
        {
            switch (varDataType.BaseType)
            {
                case BaseType.Bool:
                    return new CodeFragment($"dkx_setint4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.Short:
                    return new CodeFragment($"dkx_setint2({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.UShort:
                    return new CodeFragment($"dkx_setuns2({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.Int:
                    return new CodeFragment($"dkx_setint4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.UInt:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.Numeric:
                    return new CodeFragment($"dkx_setint{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.UNumeric:
                    return new CodeFragment($"dkx_setuns{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.Char:
                    return new CodeFragment($"dkx_setint2({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.String:
                    return new CodeFragment($"dkx_setstr({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.Date:
                    return new CodeFragment($"dkx_setdate({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.Time:
                    return new CodeFragment($"dkx_settime({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.Enum:
                    return new CodeFragment($"dkx_setuns2({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                case BaseType.Class:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
                default:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span);
            }
        }

        public static CodeFragment GenerateLeaveScope(CodeFragment objFragment)
        {
            return new CodeFragment($"dkx_release({objFragment})", objFragment.DataType, OpPrec.None, objFragment.SourceSpan);
        }

        public static CodeFragment GenerateInitializeToReference(CodeFragment objFragment)
        {
            return new CodeFragment($"dkx_addref({objFragment})", objFragment.DataType, OpPrec.None, objFragment.SourceSpan);
        }

        public static CodeFragment GenerateSwapReference(CodeFragment oldFragment, CodeFragment newFragment)
        {
            return new CodeFragment($"dkx_swap({oldFragment}, {newFragment})", newFragment.DataType, OpPrec.None, oldFragment.SourceSpan + newFragment.SourceSpan);
        }
    }
}
