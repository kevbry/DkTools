using DK.Code;
using DKX.Compilation.CodeGeneration.Constants;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Expressions
{
    class NumberChain : Chain
    {
        private string _text;
        private DataType _dataType;

        public NumberChain(string text, CodeSpan span)
            : base(span)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));

            var signed = false;
            var gotDot = false;
            byte width = 0;
            byte scale = 0;

            foreach (var ch in _text)
            {
                if (signed == false && ch == '-') signed = true;
                else if (gotDot == false && ch == '.') gotDot = true;
                else if (ch >= '0' && ch <= '9')
                {
                    width++;
                    if (gotDot) scale++;
                }
                else
                {
                    throw new ArgumentException($"'{text}' is not a valid number.");
                }
            }

            if (width == 0) throw new ArgumentException($"'{text}' is not a valid number.");

            _dataType = new DataType(signed ? BaseType.Numeric : BaseType.UNumeric, width: width, scale: scale);
        }

        public override void Report(ISourceCodeReporter reporter) { }

        public override OpCodeFragment Execute(OpCodeGeneratorContext context) => throw new OpCodeCannotBeExecutedException();

        public override OpCodeFragment ReadProvideVariable(OpCodeGeneratorContext context)
        {
            var varName = context.GetRegister(_dataType);
            return OpCodeFragment.SetVarToNumber(Span, _dataType, varName, _text);
        }

        public override OpCodeFragment ReadToVariable(OpCodeGeneratorContext context, string varName, DataType? varDataType)
        {
            return OpCodeFragment.SetVarToNumber(Span, varDataType, varName, _text);
        }

        public override ConstantValue ReadConstant(DataType constDataType)
        {
            return new NumberConstantValue(decimal.Parse(_text));
        }
    }
}
