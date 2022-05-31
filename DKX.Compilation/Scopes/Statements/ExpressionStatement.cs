using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Expressions;

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

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            if (_expression != null && !_expression.IsEmptyCode)
            {
                var fragment = _expression.ToWbdkCode_Read(context);
                cw.Write(fragment);
                cw.Write(DkxConst.StatementEndToken);
                cw.WriteLine();
            }
        }

        public override bool IsEmpty => _expression?.IsEmptyCode ?? true;
    }
}
