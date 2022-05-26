using DK.Code;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes.Statements
{
    static class StatementParser
    {
        public static async Task<Statement[]> SplitTokensIntoStatementsAsync(Scope scope, DkxTokenCollection tokens, IResolver resolver)
        {
            var stream = (tokens ?? throw new ArgumentNullException(nameof(tokens))).ToStream();
            var statements = new List<Statement>();
            ReadDataTypeResult readDataTypeResult;

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
                            statements.Add(await IfStatement.ParseAsync(scope, token.Span, stream, resolver));
                            break;
                        case DkxConst.Keywords.Return:
                            statements.Add(await ReturnStatement.ParseAsync(scope, token.Span, stream, resolver));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    continue;
                }

                if ((readDataTypeResult = await ExpressionParser.TryReadDataTypeAsync(scope, stream, resolver)).Success)
                {
                    if (stream.Peek().Type == DkxTokenType.Identifier)
                    {
                        // Variable declaration
                        statements.Add(await VariableDeclarationStatement.ParseAsync(scope, readDataTypeResult.DataType, readDataTypeResult.Span, stream, resolver));
                        continue;
                    }
                    else stream.Position = resetPos;
                }

                // Normal expression
                var statement = await TryReadExpressionStatementAsync(scope, stream, resolver);
                if (statement != null) statements.Add(statement);
                else
                {
                    var badToken = stream.Read();
                    if (!badToken.IsDefault) await scope.ReportAsync(badToken.Span, ErrorCode.SyntaxError);
                }
            }

            return statements.ToArray();
        }

        public static async Task<Statement> TryReadExpressionStatementAsync(Scope scope, DkxTokenStream stream, IResolver resolver)
        {
            var exp = await ExpressionParser.TryReadExpressionAsync(scope, stream, resolver);
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
            else await scope.ReportAsync(exp.Span, ErrorCode.StatementNotTerminated);

            return new ExpressionStatement(scope, exp);
        }

        public static async Task<Statement[]> ReadBodyOrExpressionAsync(Scope scope, DkxTokenStream stream, CodeSpan errorSpan, IResolver resolver)
        {
            var token = stream.Peek();
            if (token.IsScope)
            {
                stream.Position++;
                return (await SplitTokensIntoStatementsAsync(scope, token.Tokens, resolver)).ToArray();
            }
            else
            {
                var statement = await TryReadExpressionStatementAsync(scope, stream, resolver);
                if (statement == null)
                {
                    await scope.ReportAsync(errorSpan, ErrorCode.ExpectedExpression);
                    return Statement.EmptyArray;
                }
                return new Statement[] { statement };
            }
        }
    }
}
