using DK.Code;
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

        public override void ToCode(OpCodeGenerator code, int parentOffset) => code.WriteNumberLiteral(_text, parentOffset, Span);

        public override bool IsEmptyCode => false;

        public override void Report(ISourceCodeReporter reporter) { }
    }
}
