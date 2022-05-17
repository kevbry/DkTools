using DK;
using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Expressions
{
    class IdentifierChain : Chain
    {
        private string _name;

        public IdentifierChain(string name, CodeSpan span)
            : base(span)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
#if DEBUG
            if (!_name.IsWord()) throw new ArgumentException("Identifier name must be a single word.");
#endif
        }

        public override string ToCode(int parentOffset) => OpCodeGenerator.GenerateIdentifier(_name, parentOffset, Span);

        public override void Report(ISourceCodeReporter reporter) { }
    }
}
