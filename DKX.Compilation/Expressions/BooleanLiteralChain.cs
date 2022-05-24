using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
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

        public override bool IsEmptyCode => false;

        public override DataType DataType => DataType.Bool;

        public override DataType InferredDataType => DataType.Bool;

        public override CodeFragment ToWbdkCode_Read(ISourceCodeReporter report)
        {
            return new CodeFragment(_value ? "1" : "0", DataType.Bool, OpPrec.None, Span, readOnly: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(Span, ErrorCode.LiteralsCannotBeWrittenTo);
        }
    }
}
