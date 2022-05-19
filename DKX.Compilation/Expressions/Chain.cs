using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Expressions
{
    abstract class Chain
    {
        public abstract void ToCode(OpCodeGenerator code, int parentOffset);
        public abstract bool IsEmptyCode { get; }
        public abstract void Report(ISourceCodeReporter reporter);

        private CodeSpan _span;

        public Chain(CodeSpan span)
        {
            _span = span;
        }

        public CodeSpan Span => _span;
    }
}
