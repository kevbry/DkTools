using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
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

        public override string ToOpCodes(int parentOffset) => OpCodeGenerator.GenerateStringLiteral(_text, parentOffset, Span);

        public override void Report(ISourceCodeReporter reporter) { }
    }
}
