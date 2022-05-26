using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class ConstantChain : Chain
    {
        private Constant _constant;

        public ConstantChain(Constant constant, CodeSpan span)
            : base(span)
        {
            _constant = constant ?? throw new ArgumentNullException(nameof(constant));
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => _constant.DataType;

        public override DataType InferredDataType => _constant.DataType;

        public override Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report) => Task.FromResult(_constant.Value.ToWbdkCode());

        public override Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull)
        {
            return Task.FromResult(_constant.Value);
        }
    }
}
