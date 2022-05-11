using DK.Code;
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

        public override string ToCode() => CodeParser.StringToStringLiteral(_text);

        public override void Report(IReporter reporter) { }
    }
}
