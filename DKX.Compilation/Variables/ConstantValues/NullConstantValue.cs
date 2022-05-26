using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;
using System.Threading.Tasks;

namespace DKX.Compilation.Variables.ConstantValues
{
    class NullConstantValue : ConstantValue
    {
        public NullConstantValue(CodeSpan span) : base(span) { }

        public override bool Bool => default;
        public override char Char => default;
        public override DataType DataType => DataType.Int;
        public override bool IsBool => false;
        public override bool IsChar => false;
        public override bool IsNull => true;
        public override bool IsNumber => false;
        public override bool IsString => false;
        public override decimal Number => default;
        public override string String => default;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment("0", DataType.Int, OpPrec.None, Span, readOnly: true, constant: this);
        }

        public override async Task<ConstantValue> GetMathResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull)
        {
            await reportOrNull?.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.OperatorCannotBeUsedWithThisDataType, DataType);
            return null;
        }

        public override async Task<bool?> GetComparisonResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter reportOrNull)
        {
            await reportOrNull?.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.OperatorCannotBeUsedWithThisDataType, DataType);
            return null;
        }
    }
}
