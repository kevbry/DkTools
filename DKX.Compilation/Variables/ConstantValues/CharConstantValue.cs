using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;
using System.Threading.Tasks;

namespace DKX.Compilation.Variables.ConstantValues
{
    class CharConstantValue : ConstantValue
    {
        private char _ch;

        public CharConstantValue(char ch, CodeSpan span)
            : base(span)
        {
            _ch = ch;
        }

        public override bool Bool => default;
        public override char Char => _ch;
        public override DataType DataType => DataType.Char;
        public override bool IsBool => false;
        public override bool IsChar => true;
        public override bool IsNull => false;
        public override bool IsNumber => false;
        public override bool IsString => false;
        public override decimal Number => (decimal)_ch;
        public override string String => default;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(CodeParser.CharToCharLiteral(_ch), DataType.Char, OpPrec.None, Span, readOnly: true, constant: this);
        }

        public override async Task<bool?> GetComparisonResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull)
        {
            if (!rightValue.IsChar && !rightValue.IsNumber)
            {
                await reportOrNull?.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            if (rightValue.IsChar)
            {
                return op.GetCompareResult(((decimal)_ch).CompareTo((decimal)rightValue.Char));
            }
            else
            {
                return op.GetCompareResult(((decimal)_ch).CompareTo(rightValue.Number));
            }
        }

        public override async Task<ConstantValue> GetMathResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull)
        {
            var span = Span.Envelope(rightValue.Span);
            if (!rightValue.IsChar && rightValue.IsNumber)
            {
                await reportOrNull?.ReportAsync(span, ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            if (rightValue.IsChar)
            {
                var result = await op.GetMathResultAsync((decimal)_ch, (decimal)rightValue.Char, reportOrNull, span);
                if (result < char.MinValue || result > char.MaxValue)
                {
                    await reportOrNull?.ReportAsync(span, ErrorCode.ConstantValueOutOfRange, DataType);
                    return null;
                }
                return new CharConstantValue((char)result, span);
            }
            else
            {
                var result = await op.GetMathResultAsync((decimal)_ch, rightValue.Number, reportOrNull, span);
                if (result < char.MinValue || result > char.MaxValue)
                {
                    await reportOrNull?.ReportAsync(span, ErrorCode.ConstantValueOutOfRange, DataType);
                    return null;
                }
                return new CharConstantValue((char)result, span);
            }
        }
    }
}
