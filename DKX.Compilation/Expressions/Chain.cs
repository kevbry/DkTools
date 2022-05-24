using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Expressions
{
    abstract class Chain
    {
        public abstract DataType DataType { get; }
        public abstract DataType InferredDataType { get; }
        public abstract CodeFragment ToWbdkCode_Read(ISourceCodeReporter report);
        public abstract CodeFragment ToWbdkCode_Write(CodeFragment valueFragment, ISourceCodeReporter report);
        public abstract bool IsEmptyCode { get; }

        private CodeSpan _span;

        public Chain(CodeSpan span)
        {
            _span = span;
        }

        public CodeSpan Span => _span;
    }
}
