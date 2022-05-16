using DK.Code;
using DKX.Compilation.CodeGeneration.Constants;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
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

        public override void Report(ISourceCodeReporter reporter) { }

        public override ConstantValue ReadConstant(DataType constDataType)
        {
            return new StringConstantValue(_text);
        }

        public override OpCodeFragment ReadToVariable(OpCodeGeneratorContext context, string varName, DataType? varDataType)
        {
            return OpCodeFragment.SetVarToString(Span, varDataType, varName, _text);
        }

        public override OpCodeFragment ReadProvideVariable(OpCodeGeneratorContext context)
        {
            var reg = context.GetRegister(DataType.String255);
            return OpCodeFragment.SetVarToString(Span, DataType.String255, reg, _text);
        }

        public override OpCodeFragment Execute(OpCodeGeneratorContext context) => throw new OpCodeCannotBeExecutedException();
    }
}
