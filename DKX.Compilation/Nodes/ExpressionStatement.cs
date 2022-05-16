using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.Expressions;
using System;

namespace DKX.Compilation.Nodes
{
    class ExpressionStatement : Statement
    {
        private Chain _exp;

        public ExpressionStatement(Node parent, Chain expression)
            : base(parent, expression?.Span ?? default)
        {
            _exp = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public override OpCodeFragment Execute(OpCodeGeneratorContext context) => _exp.Execute(context);
    }
}
