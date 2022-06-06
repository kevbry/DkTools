using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;

/*
Object Reference Counter Rules:

Passing into method or property
- Caller increments ref count when passing into a function (unless unowned object reference).
- Callee decrements ref count when it exits.

Returning from method or property:
- Method increments ref count when returning (unless unowned object reference).
- Callee treats returned value as an unowned object reference.
*/

namespace DKX.Compilation.Objects
{
    static class ObjectAccess
    {
        public static CodeFragment GenerateMemberVariableGetter(CodeFragment thisFragment, uint varOffset, DataType varDataType, Span span)
        {
            switch (varDataType.BaseType)
            {
                case BaseType.Bool:
                    return new CodeFragment($"dkx_getint4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.Short:
                    return new CodeFragment($"dkx_getint2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.UShort:
                    return new CodeFragment($"dkx_getuns2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.Int:
                    return new CodeFragment($"dkx_getint4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.UInt:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.Numeric:
                    return new CodeFragment($"dkx_getint{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.UNumeric:
                    return new CodeFragment($"dkx_getuns{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.Char:
                    return new CodeFragment($"dkx_getint2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.String:
                    return new CodeFragment($"dkx_getstr({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.Date:
                    return new CodeFragment($"dkx_getdate({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.Time:
                    return new CodeFragment($"dkx_gettime({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.Enum:
                    return new CodeFragment($"dkx_getuns2({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                case BaseType.Class:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
                default:
                    return new CodeFragment($"dkx_getuns4({thisFragment}, {varOffset})", varDataType, Expressions.OpPrec.None, span, reportable: true);
            }
        }

        public static CodeFragment GenerateMemberVariableSetter(CodeFragment thisFragment, uint varOffset, DataType varDataType, Span span, CodeFragment valueFragment)
        {
            switch (varDataType.BaseType)
            {
                case BaseType.Bool:
                    return new CodeFragment($"dkx_setint4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.Short:
                    return new CodeFragment($"dkx_setint2({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.UShort:
                    return new CodeFragment($"dkx_setuns2({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.Int:
                    return new CodeFragment($"dkx_setint4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.UInt:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.Numeric:
                    return new CodeFragment($"dkx_setint{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.UNumeric:
                    return new CodeFragment($"dkx_setuns{varDataType.Width}{varDataType.Scale.ToString("00")}({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.Char:
                    return new CodeFragment($"dkx_setint2({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.String:
                    return new CodeFragment($"dkx_setstr({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.Date:
                    return new CodeFragment($"dkx_setdate({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.Time:
                    return new CodeFragment($"dkx_settime({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.Enum:
                    return new CodeFragment($"dkx_setuns2({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                case BaseType.Class:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
                default:
                    return new CodeFragment($"dkx_setuns4({thisFragment}, {varOffset}, {valueFragment})", varDataType, OpPrec.None, span, reportable: false);
            }
        }

        public static CodeFragment GenerateLeaveScope(CodeFragment objFragment)
        {
            return new CodeFragment($"dkx_release({objFragment})", objFragment.DataType, OpPrec.None, objFragment.SourceSpan, reportable: true);
        }

        public static CodeFragment GenerateInitializeToReference(CodeFragment objFragment)
        {
            if (objFragment.IsUnownedObjectReference)
            {
                // The object ref count is already incremented so no need to do so again.
                return objFragment;
            }

            return new CodeFragment($"dkx_addref({objFragment})", objFragment.DataType, OpPrec.None, objFragment.SourceSpan, reportable: true);
        }

        public static CodeFragment GenerateAddReference(CodeFragment newFragment)
        {
            return new CodeFragment($"dkx_addref({newFragment})", newFragment.DataType, OpPrec.None, newFragment.SourceSpan, reportable: true);
        }

        public static CodeFragment GenerateSwapReference(CodeFragment oldFragment, CodeFragment newFragment)
        {
            return new CodeFragment($"dkx_swap({oldFragment}, {newFragment})", newFragment.DataType, OpPrec.None, oldFragment.SourceSpan + newFragment.SourceSpan, reportable: true);
        }

        public static CodeFragment GenerateSwapNoAddReference(CodeFragment oldFragment, CodeFragment newFragment)
        {
            return new CodeFragment($"dkx_swapnoadd({oldFragment}, {newFragment})", newFragment.DataType, OpPrec.None, oldFragment.SourceSpan + newFragment.SourceSpan, reportable: true);
        }

        public static CodeFragment GenerateNewObject(IClass cls, Span span)
        {
            return new CodeFragment($"dkx_new({cls.DataSize})", new DataType(cls), OpPrec.None, span, reportable: true, flags: CodeFragmentFlags.UnownedObjectReference);
        }

        public static CodeFragment GenerateReleaseReference(CodeFragment fragment)
        {
            return new CodeFragment($"dkx_release({fragment})", fragment.DataType, OpPrec.None, fragment.SourceSpan, reportable: true);
        }

        public static CodeFragment GenerateSwapLink(CodeFragment objFragment, CodeFragment oldFragment, CodeFragment newFragment)
        {
            return new CodeFragment($"dkx_swaplink({objFragment}, {oldFragment}, {newFragment})", newFragment.DataType, OpPrec.None, oldFragment.SourceSpan + newFragment.SourceSpan, reportable: true);
        }
    }
}
