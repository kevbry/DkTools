using DKX.Compilation.Expressions;
using System;

namespace DKX.Compilation.Nodes
{
    class ExpressionStatement : Statement
    {
        private Chain _exp;

        public ExpressionStatement(Node parent, Chain expression)
            : base(parent)
        {
            _exp = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public override string ToCode() => _exp.ToCode();
    }
}
