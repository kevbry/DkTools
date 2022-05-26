using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables.ConstantValues;
using System.Threading.Tasks;

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

        public override Task<CodeFragment> ToWbdkCode_ReadAsync(ISourceCodeReporter report)
        {
            return Task.FromResult(new CodeFragment(CodeParser.CharToCharLiteral(_ch), DataType.Char, OpPrec.None, Span, readOnly: true));
        }

        public override Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override Task<ConstantValue> GetConstantOrNullAsync(ISourceCodeReporter reportOrNull)
        {
            return Task.FromResult<ConstantValue>(new CharConstantValue(_ch, Span));
        }
    }
}
