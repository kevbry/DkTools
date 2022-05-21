using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Expressions
{
    abstract class Chain
    {
        public abstract void ToCode(OpCodeGenerator code, int parentOffset);
        public abstract bool IsEmptyCode { get; }
        public abstract void Report(ISourceCodeReporter reporter);
        public abstract DataType DataType { get; }
        public abstract DataType InferredDataType { get; }

        private CodeSpan _span;

        public Chain(CodeSpan span)
        {
            _span = span;
        }

        public CodeSpan Span => _span;
    }
}
