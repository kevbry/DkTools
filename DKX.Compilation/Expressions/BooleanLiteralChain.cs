using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Expressions
{
    class BooleanLiteralChain : Chain
    {
        private bool _value;

        public BooleanLiteralChain(bool value, CodeSpan span)
            : base(span)
        {
            _value = value;
        }

        public override void ToCode(OpCodeGenerator code, int parentOffset) => code.WriteBoolLiteral(_value, parentOffset, Span);

        public override bool IsEmptyCode => false;

        public override void Report(ISourceCodeReporter reporter) { }

        public override DataType DataType => DataType.Bool;

        public override DataType InferredDataType => DataType.Bool;
    }
}
