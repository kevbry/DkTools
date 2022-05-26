using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Conversions
{
    static class ConversionValidator
    {
        public static async Task CheckConversionAsync(DataType dstDataType, CodeFragment srcFragment, ISourceCodeReporter report)
        {
            switch (TestCompatibility(dstDataType, srcFragment.DataType, srcFragment.IsConstant ? srcFragment.Constant : null))
            {
                case DataTypeCompatibility.Fail:
                    await report.ReportAsync(srcFragment.SourceSpan, ErrorCode.DataTypeNotCompatible, srcFragment.DataType.ToString(), dstDataType.ToString());
                    break;
                case DataTypeCompatibility.Warning:
                    await report.ReportAsync(srcFragment.SourceSpan, ErrorCode.DataTypeLossOfDataWarning, srcFragment.DataType.ToString(), dstDataType.ToString());
                    break;
                case DataTypeCompatibility.ConstantOutOfRange:
                    await report.ReportAsync(srcFragment.SourceSpan, ErrorCode.ConstantDoesNotFit, dstDataType.ToString());
                    break;
            }
        }

        public static DataTypeCompatibility TestCompatibility(DataType dstDataType, DataType srcDataType, ConstantValue srcConstant)
        {
            if (dstDataType.IsVoid || dstDataType.IsUnsupported) return DataTypeCompatibility.Fail;
            if (srcDataType.IsVoid || srcDataType.IsUnsupported) return DataTypeCompatibility.Fail;

            switch (dstDataType.BaseType)
            {
                case BaseType.Unsupported:
                case BaseType.Void:
                    return DataTypeCompatibility.Fail;

                case BaseType.Bool:
                    if (srcDataType.BaseType == BaseType.Bool) return DataTypeCompatibility.Good;
                    return DataTypeCompatibility.Fail;

                case BaseType.Short:
                    if (srcConstant != null && srcConstant.IsNumber)
                    {
                        var value = srcConstant.Number;
                        return value >= short.MinValue && value <= short.MaxValue ? DataTypeCompatibility.Good : DataTypeCompatibility.ConstantOutOfRange;
                    }
                    return TestNumericIntegerTypeConversion(dstDataType, srcDataType);
                case BaseType.UShort:
                    if (srcConstant != null && srcConstant.IsNumber)
                    {
                        var value = srcConstant.Number;
                        return value >= ushort.MinValue && value <= ushort.MaxValue ? DataTypeCompatibility.Good : DataTypeCompatibility.ConstantOutOfRange;
                    }
                    return TestNumericIntegerTypeConversion(dstDataType, srcDataType);
                case BaseType.Int:
                    if (srcConstant != null && srcConstant.IsNumber)
                    {
                        var value = srcConstant.Number;
                        return value >= int.MinValue && value <= int.MaxValue ? DataTypeCompatibility.Good : DataTypeCompatibility.ConstantOutOfRange;
                    }
                    return TestNumericIntegerTypeConversion(dstDataType, srcDataType);
                case BaseType.UInt:
                    if (srcConstant != null && srcConstant.IsNumber)
                    {
                        var value = srcConstant.Number;
                        return value >= uint.MinValue && value <= uint.MaxValue ? DataTypeCompatibility.Good : DataTypeCompatibility.ConstantOutOfRange;
                    }
                    return TestNumericIntegerTypeConversion(dstDataType, srcDataType);
                case BaseType.Char:
                    if (dstDataType.BaseType == BaseType.Char) return DataTypeCompatibility.Good;
                    if (srcConstant != null && srcConstant.IsNumber)
                    {
                        var value = srcConstant.Number;
                        return value >= short.MinValue && value <= short.MaxValue ? DataTypeCompatibility.Good : DataTypeCompatibility.ConstantOutOfRange;
                    }
                    return TestNumericIntegerTypeConversion(dstDataType, srcDataType);

                case BaseType.Numeric:
                case BaseType.UNumeric:
                    if (srcConstant != null && srcConstant.IsNumber)
                    {
                        var value = srcConstant.Number;
                        if (value < 0 && dstDataType.BaseType == BaseType.UNumeric) return DataTypeCompatibility.ConstantOutOfRange;
                        var digitsBelow = CountDigitsBelowDecimal(value);
                        if (digitsBelow > dstDataType.Scale) return DataTypeCompatibility.ConstantOutOfRange;
                        var digitsAbove = CountDigitsAboveDecimal(value);
                        if (digitsAbove > dstDataType.Width - dstDataType.Scale) return DataTypeCompatibility.ConstantOutOfRange;
                        return DataTypeCompatibility.Good;
                    }
                    switch (srcDataType.BaseType)
                    {
                        case BaseType.Char:
                        case BaseType.Short:
                        case BaseType.UShort:
                        case BaseType.Int:
                        case BaseType.UInt:
                            if (srcDataType.IsSigned != dstDataType.IsSigned) return DataTypeCompatibility.Warning;
                            if (srcDataType.BaseType.MaxWidth() > dstDataType.Width) return DataTypeCompatibility.Warning;
                            return DataTypeCompatibility.Good;
                        case BaseType.Numeric:
                        case BaseType.UNumeric:
                            if (srcDataType.IsSigned != dstDataType.IsSigned) return DataTypeCompatibility.Warning;
                            if (srcDataType.Width > dstDataType.Width) return DataTypeCompatibility.Warning;
                            return DataTypeCompatibility.Good;
                        case BaseType.Date:
                        case BaseType.Time:
                        case BaseType.Enum:
                            return DataTypeCompatibility.Warning;
                        default:
                            return DataTypeCompatibility.Fail;
                    }

                case BaseType.String:
                    if (srcDataType.BaseType == BaseType.String)
                    {
                        if (srcDataType.Width > dstDataType.Width) return DataTypeCompatibility.Warning;
                        return DataTypeCompatibility.Good;
                    }
                    return DataTypeCompatibility.Fail;

                case BaseType.Date:
                    switch (srcDataType.BaseType)
                    {
                        case BaseType.Date:
                            return DataTypeCompatibility.Good;
                        case BaseType.Short:
                        case BaseType.UShort:
                        case BaseType.Int:
                        case BaseType.UInt:
                        case BaseType.Numeric:
                        case BaseType.UNumeric:
                        case BaseType.Char:
                        case BaseType.Time:
                        case BaseType.Enum:
                            return DataTypeCompatibility.Warning;
                        default:
                            return DataTypeCompatibility.Fail;
                    }

                case BaseType.Time:
                    switch (srcDataType.BaseType)
                    {
                        case BaseType.Time:
                            return DataTypeCompatibility.Good;
                        case BaseType.Short:
                        case BaseType.UShort:
                        case BaseType.Int:
                        case BaseType.UInt:
                        case BaseType.Numeric:
                        case BaseType.UNumeric:
                        case BaseType.Char:
                        case BaseType.Date:
                        case BaseType.Enum:
                            return DataTypeCompatibility.Warning;
                        default:
                            return DataTypeCompatibility.Fail;
                    }

                case BaseType.Enum:
                    switch (srcDataType.BaseType)
                    {
                        case BaseType.Enum:
                            var dstOptions = dstDataType.Options;
                            foreach (var srcOption in srcDataType.Options)
                            {
                                if (!dstOptions.Any(x => x == srcOption)) return DataTypeCompatibility.Warning;
                            }
                            return DataTypeCompatibility.Good;
                        case BaseType.Short:
                        case BaseType.UShort:
                        case BaseType.Int:
                        case BaseType.UInt:
                        case BaseType.Numeric:
                        case BaseType.UNumeric:
                        case BaseType.Char:
                        case BaseType.Date:
                        case BaseType.Time:
                            return DataTypeCompatibility.Warning;
                        default:
                            return DataTypeCompatibility.Fail;
                    }

                case BaseType.Table:
                case BaseType.Indrel:
                    if (srcDataType.BaseType == dstDataType.BaseType) return DataTypeCompatibility.Good;
                    return DataTypeCompatibility.Fail;

                case BaseType.Class:
                    if (srcConstant != null && srcConstant.IsNull) return DataTypeCompatibility.Good;
                    if (srcDataType.BaseType == BaseType.Class)
                    {
                        if (srcDataType.Class.FullClassName == dstDataType.Class.FullClassName) return DataTypeCompatibility.Good;
                    }
                    return DataTypeCompatibility.Fail;

                case BaseType.Variant:
                    if (srcDataType.BaseType == BaseType.Variant) return DataTypeCompatibility.Good;
                    return DataTypeCompatibility.Fail;

                default:
                    throw new InvalidBaseTypeException(dstDataType.BaseType);
            }
        }

        private static DataTypeCompatibility TestNumericIntegerTypeConversion(DataType dstDataType, DataType srcDataType)
        {
            if (srcDataType.BaseType == dstDataType.BaseType) return DataTypeCompatibility.Good;
            switch (srcDataType.BaseType)
            {
                case BaseType.Short:
                case BaseType.UShort:
                case BaseType.Int:
                case BaseType.UInt:
                case BaseType.Char:
                    if (srcDataType.NumBits <= dstDataType.NumBits && srcDataType.IsSigned == dstDataType.IsSigned) return DataTypeCompatibility.Good;
                    return DataTypeCompatibility.Warning;
                case BaseType.Numeric:
                    if (srcDataType.IsSigned != dstDataType.IsSigned) return DataTypeCompatibility.Warning;
                    if (srcDataType.Width > dstDataType.BaseType.MaxWidth()) return DataTypeCompatibility.Warning;
                    return DataTypeCompatibility.Good;
                case BaseType.Date:
                case BaseType.Time:
                case BaseType.Enum:
                    return DataTypeCompatibility.Warning;
                default:
                    return DataTypeCompatibility.Fail;
            }
        }

        private static int CountDigitsAboveDecimal(decimal value)
        {
            value = Math.Abs(value);
            if (value < 1M) return 1;

            var count = 0;
            while (value >= 1M)
            {
                count++;
                value /= 10M;
            }

            return count;
        }

        private static int CountDigitsBelowDecimal(decimal value)
        {
            value = Math.Abs(value);

            var count = 0;
            while (Math.Floor(value) != value)
            {
                count++;
                value *= 10;
            }

            return count;
        }
    }

    enum DataTypeCompatibility
    {
        Good,
        Warning,
        Fail,
        ConstantOutOfRange
    }
}
