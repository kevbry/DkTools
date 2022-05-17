using DK.Code;
using DKX.Compilation.CodeGeneration.Constants;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Expressions
{
    abstract class Chain
    {
        public abstract void Report(ISourceCodeReporter reporter);

        public abstract OpCodeFragment Execute(OpCodeGeneratorContext context);
        public abstract OpCodeFragment ReadToVariable(OpCodeGeneratorContext context, string varName, DataType? varDataType);
        public abstract OpCodeFragment ReadProvideVariable(OpCodeGeneratorContext context);
        public abstract ConstantValue ReadConstant(DataType constDataType);

        private CodeSpan _span;

        public Chain(CodeSpan span)
        {
            _span = span;
        }

        public CodeSpan Span => _span;
    }
}
