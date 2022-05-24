using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Expressions
{
    class StringLiteralChain : Chain
    {
        private string _text;

        public StringLiteralChain(string text, CodeSpan span)
            : base(span)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => new DataType(BaseType.String, width: (byte)(_text.Length == 0 ? 1 : _text.Length));

        public override DataType InferredDataType => DataType.String255;

        public override CodeFragment ToWbdkCode_Read(ISourceCodeReporter report)
        {
            return new CodeFragment(CodeParser.StringToStringLiteral(_text), DataType, OpPrec.None, Span, readOnly: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeFragment valueFragment, ISourceCodeReporter report)
        {
            throw new CodeException(Span, ErrorCode.LiteralsCannotBeWrittenTo);
        }
    }
}
