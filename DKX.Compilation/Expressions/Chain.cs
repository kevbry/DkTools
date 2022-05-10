using DK.Code;

namespace DKX.Compilation.Expressions
{
    abstract class Chain
    {
        public abstract string ToCode();
        public abstract void Report(IReporter reporter);

        private CodeSpan _span;

        public Chain(CodeSpan span)
        {
            _span = span;
        }

        public CodeSpan Span => _span;

        public override string ToString() => ToCode();
    }
}
