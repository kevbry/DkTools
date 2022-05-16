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

        public override OpCodeFragment Execute(OpCodeGeneratorContext context) => _exp.ReadToVariable(context, _variable.Name, _variable.DataType);
    }
}
