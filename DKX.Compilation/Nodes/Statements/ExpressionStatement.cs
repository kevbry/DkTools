using DKX.Compilation.CodeGeneration.OpCodes;
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

        public override void ToCode(OpCodeGenerator code, int parentOffset) => _exp.ToCode(code, parentOffset);

        public override bool IsEmptyCode => _exp.IsEmptyCode;
    }
}
