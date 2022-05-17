using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.Expressions
{
    class VariableChain : Chain
    {
        private Variable _variable;

        public VariableChain(Variable variable, CodeSpan span)
            : base(span)
        {
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        public override string ToCode(int parentOffset)
        {
            return OpCodeGenerator.GenerateVariable(_variable.Name, parentOffset, Span);
        }

        public override void Report(ISourceCodeReporter reporter) { }
    }
}
