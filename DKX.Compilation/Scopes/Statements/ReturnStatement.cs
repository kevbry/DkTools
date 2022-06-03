using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
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

        public static ReturnStatement Parse(Scope parent, DkxTokenCollection tokens)
        {
            if (tokens.Count == 0 || !tokens[0].IsKeyword(DkxConst.Keywords.Return)) throw new InvalidOperationException("Expected first token to be the 'return' keyword.");
            var keywordToken = tokens[0];

            var ret = new ReturnStatement(parent, keywordToken.Span);

            try
            {
                var returnScope = ret.GetScope<IReturnScope>();
                if (returnScope == null) throw new InvalidOperationException("Could not get return scope.");
                ret._dataType = returnScope.ReturnDataType;

                if (ret._dataType.IsVoid)
                {
                    if (tokens.Count == 1)
                    {
                        throw new CodeException(keywordToken.Span, ErrorCode.ExpectedToken, ';');
                    }
                    else
                    {
                        if (!tokens[1].IsStatementEnd) throw new CodeException(tokens[0].Span, ErrorCode.ExpectedToken, ';');
                        if (tokens.Count > 2) throw new CodeException(tokens[1].Span, ErrorCode.SyntaxError);
                    }
                }
                else
                {
                    var stream = new DkxTokenStream(tokens, 1);
                    var expression = ExpressionParser.ReadExpressionOrNull(ret, stream);
                    if (expression == null) throw new CodeException(keywordToken.Span, ErrorCode.ExpectedExpression);

                    if (stream.EndOfStream || !stream.Peek().IsStatementEnd) throw new CodeException(stream.Read().Span, ErrorCode.ExpectedToken, ';');
                    stream.Position++;
                    if (!stream.EndOfStream) throw new CodeException(stream.Read().Span, ErrorCode.SyntaxError);
                    ret._expression = expression;
                }
            }
            catch (CodeException ex)
            {
                ret.AddReportItem(ex.ToReportItem());
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
