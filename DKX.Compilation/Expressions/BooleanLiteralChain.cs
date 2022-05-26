using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class BooleanLiteralChain : Chain
    {
        private bool _value;

        public BooleanLiteralChain(bool value, CodeSpan span)
            : base(span)
        {
            _value = value;
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => DataType.Bool;

        public override DataType InferredDataType => DataType.Bool;

        public override Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            return Task.FromResult(new CodeFragment(_value ? "1" : "0", DataType.Bool, OpPrec.None, Span, readOnly: true));
        }

        public override Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull)
        {
            return Task.FromResult<ConstantValue>(new BoolConstantValue(_value, Span));
        }
    }
}
