using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes.Statements
{
    class ReturnStatement : Statement
    {
        private DataType _dataType;
        private Chain _expression;

        private ReturnStatement(Scope parent, CodeSpan keywordSpan) : base(parent, keywordSpan) { }

        public override bool IsEmpty => false;

        public static async Task<ReturnStatement> ParseAsync(Scope parent, CodeSpan keywordSpan, DkxTokenStream stream, IResolver resolver)
        {
            var ret = new ReturnStatement(parent, keywordSpan);

            var returnScope = ret.GetScope<IReturnScope>();
            if (returnScope == null) throw new InvalidOperationException("Could not get return scope.");
            ret._dataType = returnScope.ReturnDataType;

            if (ret._dataType.IsVoid)
            {
                if (!stream.Peek().IsStatementEnd) await ret.ReportAsync(keywordSpan, ErrorCode.ExpectedToken, ';');
                else stream.Position++;
            }
            else
            {
                var expression = await ExpressionParser.TryReadExpressionAsync(ret, stream, resolver);
                if (expression == null) await ret.ReportAsync(keywordSpan, ErrorCode.ExpectedExpression);
                ret._expression = expression;
            }

            return ret;
        }

        internal override async Task GenerateWbdkCodeAsync(CodeWriter cw)
        {
            cw.Write("return");
            if (_expression != null)
            {
                cw.Write(' ');
                cw.Write(await _expression.ToWbdkCode_ReadAsync(this));
            }
            cw.Write(';');
            cw.WriteLine();
        }
    }
}
