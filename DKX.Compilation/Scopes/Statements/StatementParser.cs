using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Scopes.Statements
{
    static class StatementParser
    {
        public static Statement[] SplitTokensIntoStatements(Scope scope, DkxTokenCollection tokens)
        {
            var stream = (tokens ?? throw new ArgumentNullException(nameof(tokens))).ToStream();
            var statements = new List<Statement>();

            while (!stream.EndOfStream)
            {
                var resetPos = stream.Position;
                var token = stream.Peek();
                if (token.Type == DkxTokenType.Keyword && DkxConst.Keywords.ControlStatementStartKeyword.Contains(token.Text))
                {
                    // Control statement
                    stream.Position++;
                    switch (token.Text)
                    {
                        case DkxConst.Keywords.If:
                            statements.Add(IfStatement.Parse(scope, token.Span, stream));
                            break;
                        case DkxConst.Keywords.Return:
                            statements.Add(ReturnStatement.Parse(scope, token.Span, stream));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    continue;
                }

                if (ExpressionParser.TryReadDataType(scope, stream, out var dataType, out var dataTypeSpan))
                {
                    if (stream.Peek().Type == DkxTokenType.Identifier)
                    {
                        // Variable declaration
                        statements.Add(VariableDeclarationStatement.Parse(scope, dataType, dataTypeSpan, stream, scope.Resolver));
                        continue;
                    }
                    else stream.Position = resetPos;
                }

                // Normal expression
                var statement = TryReadExpressionStatement(scope, stream);
                if (statement != null) statements.Add(statement);
                else
                {
                    var badToken = stream.Read();
                    if (!badToken.IsNone) scope.Report(badToken.Span, ErrorCode.SyntaxError);
                }
            }

            return statements.ToArray();
        }

        public static Statement TryReadExpressionStatement(Scope scope, DkxTokenStream stream)
        {
            var exp = ExpressionParser.TryReadExpression(scope, stream);
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
            else scope.Report(exp.Span, ErrorCode.StatementNotTerminated);

            return new ExpressionStatement(scope, exp);
        }

        public static Statement[] ReadBodyOrExpression(Scope scope, DkxTokenStream stream, Span errorSpan)
        {
            var token = stream.Peek();
            if (token.IsScope)
            {
                stream.Position++;
                return (SplitTokensIntoStatements(scope, token.Tokens)).ToArray();
            }
            else
            {
                var statement = TryReadExpressionStatement(scope, stream);
                if (statement == null)
                {
                    scope.Report(errorSpan, ErrorCode.ExpectedExpression);
                    return Statement.EmptyArray;
                }
                return new Statement[] { statement };
            }
        }
    }
}
