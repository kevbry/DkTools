using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Expressions
{
    class CharLiteralChain : Chain
    {
        private char _ch;

        public CharLiteralChain(char ch, CodeSpan span)
            : base(span)
        {
            _ch = ch;
        }

        public override DataType DataType => DataType.Char;

        public override DataType InferredDataType => DataType.Char;

        public override void ToCode(OpCodeGenerator code, int parentOffset) => code.WriteCharLiteral(_ch, parentOffset, Span);

        public override bool IsEmptyCode => false;

        public override void Report(ISourceCodeReporter reporter) { }
    }
}
