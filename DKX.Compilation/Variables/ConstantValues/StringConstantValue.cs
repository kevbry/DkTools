using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;
using System.Threading.Tasks;

namespace DKX.Compilation.Variables.ConstantValues
{
    class StringConstantValue : ConstantValue
    {
        private string _text;
        private DataType _dataType;

        public StringConstantValue(string text, DataType dataType, CodeSpan span)
            : base(span)
        {
            _text = text;
            _dataType = dataType;
        }

        public override bool Bool => default;
        public override char Char => default;
        public override DataType DataType => _dataType;
        public override bool IsBool => false;
        public override bool IsChar => false;
        public override bool IsNull => false;
        public override bool IsNumber => false;
        public override bool IsString => true;
        public override decimal Number => default;
        public override string String => _text;

        public override CodeFragment ToWbdkCode()
        {
            return new CodeFragment(CodeParser.StringToStringLiteral(_text), _dataType, Expressions.OpPrec.None, Span, readOnly: true, constant: this);
        }

        public override async Task<bool?> GetComparisonResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter report)
        {
            if (!rightValue.IsString)
            {
                await report.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.DataTypeNotCompatible, rightValue.DataType, DataType);
                return null;
            }

            return op.GetCompareResult(string.Compare(_text, rightValue.String, ignoreCase: true));
        }

        public override async Task<ConstantValue> GetMathResultOrNullAsync(Operator op, ConstantValue rightValue, ISourceCodeReporter report)
        {
            await report.ReportAsync(Span.Envelope(rightValue.Span), ErrorCode.OperatorCannotBeUsedWithThisDataType);
            return null;
        }
    }
}
