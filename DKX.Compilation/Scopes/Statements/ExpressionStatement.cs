using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Expressions;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes.Statements
{
    class ExpressionStatement : Statement
    {
        private Chain _expression;

        public ExpressionStatement(Scope parent, Chain expression)
            : base(parent, expression.Span)
        {
            _expression = expression;
        }

        internal override async Task GenerateWbdkCodeAsync(CodeWriter cw)
        {
            if (_expression != null && !_expression.IsEmptyCode)
            {
                var fragment = await _expression.ToWbdkCode_ReadAsync(this);
                cw.Write(fragment);
                cw.Write(DkxConst.StatementEndToken);
                cw.WriteLine();
            }
        }

        public override bool IsEmpty => _expression?.IsEmptyCode ?? true;
    }
}
