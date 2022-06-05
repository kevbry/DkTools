using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Expressions
{
    class StringLiteralChain : Chain
    {
        private string _text;

        public StringLiteralChain(string text, Span span)
            : base(span)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => new DataType(BaseType.String, width: (byte)(_text.Length == 0 ? 1 : _text.Length));

        public override DataType InferredDataType => DataType.String255;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            return new CodeFragment(CodeParser.StringToStringLiteral(_text), DataType, OpPrec.None, Span);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report)
        {
            return new ConstValueTerm(new StringConstValue(_text, DataType, Span), Span);
        }
    }
}
