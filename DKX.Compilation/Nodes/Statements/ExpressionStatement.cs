using DKX.Compilation.Expressions;
using System;

namespace DKX.Compilation.Nodes.Statements
{
    class ExpressionStatement : Statement
    {
        private Chain _exp;

        public ExpressionStatement(Node parent, Chain expression)
            : base(parent, expression?.Span ?? default)
        {
            _exp = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public override string ToCode(int parentOffset) => _exp.ToOpCodes(parentOffset);
    }
}
