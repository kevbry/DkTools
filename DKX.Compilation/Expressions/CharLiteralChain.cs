using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;

namespace DKX.Compilation.Expressions
{
    class CharLiteralChain : Chain
    {
        private char _ch;

        public CharLiteralChain(char ch, Span span)
            : base(span)
        {
            _ch = ch;
        }

        public override DataType DataType => DataType.Char;

        public override DataType InferredDataType => DataType.Char;

        public override bool IsEmptyCode => false;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context)
        {
            return new CodeFragment(CodeParser.CharToCharLiteral(_ch), DataType.Char, OpPrec.None, Span, readOnly: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report)
        {
            return new ConstValueTerm(new CharConstValue(_ch, Span), Span);
        }
    }
}
