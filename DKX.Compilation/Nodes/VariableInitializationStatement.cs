using DKX.Compilation.Expressions;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.Nodes
{
    class VariableInitializationStatement : Statement
    {
        private Variable _variable;
        private Chain _exp;

        public VariableInitializationStatement(Node parent, Variable variable, Chain exp)
            : base(parent)
        {
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));
            _exp = exp ?? throw new ArgumentNullException(nameof(exp));
        }

        public override string ToCode() => $"asn(@{_variable.Name},{_exp.ToCode()})";
    }
}
