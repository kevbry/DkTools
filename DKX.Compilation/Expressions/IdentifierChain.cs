using DK;
using DK.Code;
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

        public override string ToCode() => "@" + _name;

        public override void Report(IReporter reporter) { }
    }
}
