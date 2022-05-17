using DK.Code;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Expressions
{
    abstract class Chain
    {
        public abstract string ToCode(int parentOffset);
        public abstract void Report(ISourceCodeReporter reporter);

        private CodeSpan _span;

        public Chain(CodeSpan span)
        {
            _span = span;
        }

        public CodeSpan Span => _span;

        public override string ToString() => ToCode(-1);
    }
}
