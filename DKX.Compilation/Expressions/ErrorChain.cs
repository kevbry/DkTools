using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Expressions
{
    class ErrorChain : Chain
    {
        private Chain _innerChain;
        private ErrorCode _errorCode;
        private object[] _args;

        public ErrorChain(Chain innerChainOrNull, CodeSpan span, ErrorCode errorCode, params object[] args)
            : base(span)
        {
            _innerChain = innerChainOrNull;
            _errorCode = errorCode;
            _args = args;
        }

        public ErrorChain(Chain innerChainOrNull, int pos, ErrorCode errorCode, params object[] args)
            : base(new CodeSpan(pos, pos))
        {
            _innerChain = innerChainOrNull;
            _errorCode = errorCode;
            _args = args;
        }

        public override void Report(ISourceCodeReporter reporter) => reporter.ReportItem(Span, _errorCode, _args);

        public override void ToCode(OpCodeGenerator code, int parentOffset) => _innerChain?.ToCode(code, parentOffset);

        public override bool IsEmptyCode => _innerChain?.IsEmptyCode ?? true;

        public override DataType DataType => _innerChain?.DataType ?? DataType.Int;

        public override DataType InferredDataType => _innerChain?.InferredDataType ?? DataType.Int;
    }
}
