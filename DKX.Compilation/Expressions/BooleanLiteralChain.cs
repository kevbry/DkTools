using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;

namespace DKX.Compilation.Expressions
{
    class BooleanLiteralChain : Chain
    {
        private bool _value;

        public BooleanLiteralChain(bool value, Span span)
            : base(span)
        {
            _value = value;
        }

        public override bool IsEmptyCode => false;

        public override DataType DataType => DataType.Bool;

        public override DataType InferredDataType => DataType.Bool;

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            return new CodeFragment(_value ? "1" : "0", DataType.Bool, OpPrec.None, Span, readOnly: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report)
        {
            return new ConstValueTerm(new BoolConstValue(_value, Span), Span);
        }
    }
}
