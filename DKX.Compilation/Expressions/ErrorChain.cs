using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    class ErrorChain : Chain
    {
        private Chain _innerChain;

        public ErrorChain(Chain innerChainOrNull, CodeSpan span)
            : base(span)
        {
            _innerChain = innerChainOrNull;
        }

        public override DataType DataType => _innerChain?.DataType ?? DataType.Int;
        public override DataType InferredDataType => _innerChain?.InferredDataType ?? DataType.Int;
        public override bool IsEmptyCode => _innerChain?.IsEmptyCode ?? true;

        public override Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            throw new InvalidOperationException("An error chain should never reach the point of generating code.");
        }

        public override Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new InvalidOperationException("An error chain should never reach the point of generating code.");
        }

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull) => Task.FromResult<ConstantValue>(null);
    }
}
