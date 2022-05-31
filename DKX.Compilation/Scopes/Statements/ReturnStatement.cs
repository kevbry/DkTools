using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using System;

namespace DKX.Compilation.Scopes.Statements
{
    class ReturnStatement : Statement
    {
        private DataType _dataType;
        private Chain _expression;

        private ReturnStatement(Scope parent, Span keywordSpan) : base(parent, keywordSpan) { }

        public override bool IsEmpty => false;

        public static ReturnStatement Parse(Scope parent, Span keywordSpan, DkxTokenStream stream, IResolver resolver)
        {
            var ret = new ReturnStatement(parent, keywordSpan);

            var returnScope = ret.GetScope<IReturnScope>();
            if (returnScope == null) throw new InvalidOperationException("Could not get return scope.");
            ret._dataType = returnScope.ReturnDataType;

            if (ret._dataType.IsVoid)
            {
                if (!stream.Peek().IsStatementEnd) ret.Report(keywordSpan, ErrorCode.ExpectedToken, ';');
                else stream.Position++;
            }
            else
            {
                var expression = ExpressionParser.TryReadExpression(ret, stream, resolver);
                if (expression == null) ret.Report(keywordSpan, ErrorCode.ExpectedExpression);
                ret._expression = expression;
            }

            return ret;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            cw.Write("return");
            if (_expression != null)
            {
                cw.Write(' ');
                cw.Write(_expression.ToWbdkCode_Read(context));
            }
            cw.Write(';');
            cw.WriteLine();
        }
    }
}
