using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.Nodes
{
    class VariableInitializationStatement : Statement
    {
        private Variable _variable;
        private Chain _exp;

        public VariableInitializationStatement(Node parent, Variable variable, Chain exp, CodeSpan span)
            : base(parent, span)
        {
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));
            _exp = exp ?? throw new ArgumentNullException(nameof(exp));
        }

        public override string ToCode(int parentOffset)
        {
            return string.Concat(
                OpCodeGenerator.GenerateOpCode("asn", parentOffset, Span),
                "(",
                OpCodeGenerator.GenerateVariable(_variable.Name, Span.Start, Span),
                ",",
                _exp.ToCode(Span.Start),
                ")");
        }
    }
}
