using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
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

        public override bool IsEmptyCode => false;

        public override CodeFragment ToWbdkCode_Read(ISourceCodeReporter report)
        {
            return new CodeFragment(CodeParser.CharToCharLiteral(_ch), DataType.Char, OpPrec.None, Span, readOnly: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(Span, ErrorCode.LiteralsCannotBeWrittenTo);
        }
    }
}
