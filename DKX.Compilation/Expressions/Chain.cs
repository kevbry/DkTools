using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstTerms;

namespace DKX.Compilation.Expressions
{
    abstract class Chain
    {
        public abstract DataType DataType { get; }
        public abstract DataType InferredDataType { get; }
        public abstract CodeFragment ToWbdkCode_Read(CodeGenerationContext context);
        public abstract CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment);
        public abstract bool IsEmptyCode { get; }
        public abstract ConstTerm ToConstTermOrNull(IReportItemCollector reportOrNull);

        public static readonly Chain[] EmptyArray = new Chain[0];

        private Span _span;

        public Chain(Span span)
        {
            _span = span;
        }

        public virtual bool IsStatic => false;
        public Span Span => _span;
    }
}
