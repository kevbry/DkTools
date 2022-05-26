using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;
using System.Threading.Tasks;

namespace DKX.Compilation.Variables.ConstantValues
{
    class BoolConstantValue : ConstantValue
    {
        private bool _value;

        public BoolConstantValue(bool value, CodeSpan span)
            : base(span)
        {
            _value = value;
        }

        public override bool Bool => default;
        public override char Char => default;
        public override DataType DataType => DataType.Bool;
        public override bool IsBool => false;
        public override bool IsChar => false;
        public override bool IsNull => false;
        public override bool IsNumber => false;
        public override bool IsString => false;
        public override decimal Number => default;
        public override string String => default;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(_value ? "1" : "0", DataType.Bool, Expressions.OpPrec.None, Span, readOnly: true, constant: this);
        }

        public override async Task<bool?> GetComparisonResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull)
        {
            if (!rightValue.IsBool)
            {
                await reportOrNull?.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            switch (op)
            {
                case Operator.Equal:
                    return _value == rightValue.Bool;
                case Operator.NotEqual:
                    return _value != rightValue.Bool;
                default:
                    await reportOrNull?.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.OperatorCannotBeUsedWithThisDataType, DataType);
                    return null;
            }
        }

        public override async Task<ConstantValue> GetMathResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull)
        {
            await reportOrNull?.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.OperatorCannotBeUsedWithThisDataType, DataType);
            return null;
        }
    }
}
