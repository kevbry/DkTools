using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Expressions
{
    class ErrorChain : Chain
    {
        private Chain _innerChain;

        public ErrorChain(Chain innerChainOrNull, Span span)
            : base(span)
        {
            _innerChain = innerChainOrNull;
        }

        public override DataType DataType => _innerChain?.DataType ?? DataType.Int;
        public override DataType InferredDataType => _innerChain?.InferredDataType ?? DataType.Int;
        public override bool IsEmptyCode => _innerChain?.IsEmptyCode ?? true;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context)
        {
            throw new InvalidOperationException("An error chain should never reach the point of generating code.");
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment)
        {
            throw new InvalidOperationException("An error chain should never reach the point of generating code.");
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report) => new ConstErrorTerm(Span);
    }
}
