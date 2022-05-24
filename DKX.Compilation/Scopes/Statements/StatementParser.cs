using DK.Code;
using DKX.Compilation.Expressions;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Scopes.Statements
{
    static class StatementParser
    {
        public static Statement[] SplitTokensIntoStatements(Scope parentScope, DkxTokenCollection tokens)
        {
            var stream = (tokens ?? throw new ArgumentNullException(nameof(tokens))).ToStream();
            var statements = new List<Statement>();

            while (!stream.EndOfStream)
            {
                if (stream.Test(t => t.Type == DkxTokenType.Keyword && DkxConst.Keywords.ControlStatementStartKeyword.Contains(t.Text)))
                {
                    var token = stream.Read();
                    switch (token.Text)
                    {
                        case DkxConst.Keywords.If:
                            statements.Add(new IfStatement(parentScope, token.Span, stream));
                            break;
                        case DkxConst.Keywords.Return:
                            statements.Add(new ReturnStatement(parentScope, token.Span, stream));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    var statement = ReadExpressionStatementOrNull(parentScope, stream);
                    if (statement != null) statements.Add(statement);
                    else break;
                }
            }

            return statements.ToArray();
        }

        public static Statement ReadExpressionStatementOrNull(Scope scope, DkxTokenStream stream)
        {
            var exp = ExpressionParser.ReadExpressionOrNull(scope, stream);
            if (exp == null)
            {
                if (stream.Peek().IsStatementEnd)
                {
                    var endToken = stream.Read();
                    return new EmptyStatement(scope, endToken.Span);
                }
                return null;
            }

            var token = stream.Peek();
            if (token.IsStatementEnd) stream.Position++;
            else scope.ReportItem(exp.Span, ErrorCode.StatementNotTerminated);

            return new ExpressionStatement(scope, exp);
        }

        public static Statement[] ReadBodyOrExpression(Scope scope, DkxTokenStream stream, CodeSpan errorSpan)
        {
            var token = stream.Peek();
            if (token.IsScope)
            {
                stream.Position++;
                return SplitTokensIntoStatements(scope, token.Tokens).ToArray();
            }
            else
            {
                var statement = ReadExpressionStatementOrNull(scope, stream);
                if (statement == null)
                {
                    scope.ReportItem(errorSpan, ErrorCode.ExpectedExpression);
                    return Statement.EmptyArray;
                }
                return new Statement[] { statement };
            }
        }
    }
}
