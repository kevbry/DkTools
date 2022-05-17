using DK.Code;
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

        public override string ToCode(int offset) => _innerChain?.ToCode(offset);
    }
}
