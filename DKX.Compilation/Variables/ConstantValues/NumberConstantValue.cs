using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;
using System.Threading.Tasks;

namespace DKX.Compilation.Variables.ConstantValues
{
    class NumberConstantValue : ConstantValue
    {
        private decimal _value;
        private DataType _dataType;

        public NumberConstantValue(decimal value, DataType dataType, CodeSpan span)
            : base(span)
        {
            _value = value;
            _dataType = dataType;
        }

        public override bool Bool => false;
        public override char Char => default;
        public override DataType DataType => _dataType;
        public override bool IsBool => false;
        public override bool IsChar => false;
        public override bool IsNull => false;
        public override bool IsNumber => true;
        public override bool IsString => false;
        public override decimal Number => _value;
        public override string String => default;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(_value.ToString(), _dataType, Expressions.OpPrec.None, Span, readOnly: true, constant: this);
        }

        public override async Task<ConstantValue> GetMathResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull)
        {
            var span = Span.Envelope(rightValue.Span);

            if (rightValue.IsNumber)
            {
                var result = await op.GetMathResultAsync(_value, rightValue.Number, reportOrNull, span);
                if (result < _dataType.MinNumber || result > _dataType.MaxNumber) await reportOrNull?.ReportAsync(Span, ErrorCode.ConstantValueOutOfRange, _dataType);
                return new NumberConstantValue(result, _dataType, span);
            }

            if (rightValue.IsChar)
            {
                var result = await op.GetMathResultAsync(_value, (decimal)rightValue.Char, reportOrNull, span);
                if (result < _dataType.MinNumber || result > _dataType.MaxNumber) await reportOrNull?.ReportAsync(Span, ErrorCode.ConstantValueOutOfRange, _dataType);
                return new NumberConstantValue(result, _dataType, span);
            }

            await reportOrNull?.ReportAsync(span, ErrorCode.OperatorCannotBeUsedWithThisDataType, rightValue.DataType);
            return null;
        }

        public override async Task<bool?> GetComparisonResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull)
        {
            if (rightValue.IsNumber)
            {
                return op.GetCompareResult(decimal.Compare(_value, rightValue.Number));
            }

            if (rightValue.IsChar)
            {
                return op.GetCompareResult(decimal.Compare(_value, (decimal)rightValue.Char));
            }

            await reportOrNull?.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.OperatorCannotBeUsedWithThisDataType, rightValue.DataType);
            return null;
        }
    }
}
