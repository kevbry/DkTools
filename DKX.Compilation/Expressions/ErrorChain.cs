using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using System;

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

        public override bool IsEmptyCode => _innerChain?.IsEmptyCode ?? true;

        public override DataType DataType => _innerChain?.DataType ?? DataType.Int;

        public override DataType InferredDataType => _innerChain?.InferredDataType ?? DataType.Int;

        public override CodeFragment ToWbdkCode_Read(ISourceCodeReporter report)
        {
            throw new InvalidOperationException("An error chain should never reach the point of generating code.");
        }

        public override CodeFragment ToWbdkCode_Write(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new InvalidOperationException("An error chain should never reach the point of generating code.");
        }
    }
}
