using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
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
            if (tokens.Count == 0) return Statement.EmptyArray;

            var stmts = new List<Statement>();

            try
            {
                var pos = 0;
                do
                {
                    var nextPos = FindStatementEnd(tokens, pos, allowControlStatements: true);
                    var stmtTokens = tokens.GetRange(pos, nextPos < 0 ? -1 : nextPos - pos);
                    var stmt = TokensToStatementOrNull(scope, stmtTokens);
                    if (stmt != null) stmts.Add(stmt);
                    pos = nextPos;
                }
                while (pos >= 0 && pos < tokens.Count);
            }
            catch (CodeException ex)
            {
                scope.AddReportItem(ex.ToReportItem());
            }

            return stmts.ToArray();
        }

        public static int FindStatementEnd(DkxTokenCollection tokens, int startPos, bool allowControlStatements)
        {
            var pos = startPos;
            var token = tokens[pos];

            if (allowControlStatements)
            {
                if (token.Type == DkxTokenType.Keyword)
                {
                    #region if
                    if (token.IsKeyword(DkxConst.Keywords.If))
                    {
                        pos++;
                        if (tokens[pos].IsBrackets)
                        {
                            pos++;
                            if (tokens[pos].IsScope) pos++;
                            else
                            {
                                pos = FindStatementEnd(tokens, pos, allowControlStatements: true);
                                if (pos < 0) return -1;
                            }

                            while (true)
                            {
                                if (tokens[pos].IsKeyword(DkxConst.Keywords.Else))
                                {
                                    pos++;
                                    if (tokens[pos].IsKeyword(DkxConst.Keywords.If))
                                    {
                                        pos++;
                                        if (tokens[pos].IsBrackets)
                                        {
                                            pos++;
                                            if (tokens[pos].IsScope) pos++;
                                            else
                                            {
                                                pos = FindStatementEnd(tokens, pos, allowControlStatements: true);
                                                if (pos < 0) return -1;
                                            }
                                        }
                                        else return pos;    // No condition
                                    }
                                    else if (tokens[pos].IsScope) pos++;
                                    else
                                    {
                                        pos = FindStatementEnd(tokens, pos, allowControlStatements: true);
                                        if (pos < 0) return -1;
                                    }
                                }
                                else return pos;    // No else
                            }
                        }
                        else return pos;    // No condition
                    }
                    #endregion
                    #region return
                    if (token.IsKeyword(DkxConst.Keywords.Return))
                    {
                        return FindStatementEnd(tokens, pos + 1, allowControlStatements: false);
                    }
                    #endregion
                    #region var
                    if (token.IsKeyword(DkxConst.Keywords.Var))
                    {
                        return FindStatementEnd(tokens, pos + 1, allowControlStatements: false);
                    }
                    #endregion
                }
            }

            var endPos = tokens.FindIndex(t => t.IsStatementEnd || (t.Type == DkxTokenType.Keyword && DkxConst.Keywords.ControlStatementStartKeyword.Contains(t.Text)), startPos);
            if (endPos < 0) return -1;
            if (tokens[endPos].IsStatementEnd) endPos++;
            return endPos < tokens.Count ? endPos : -1;
        }

        private static Statement TokensToStatementOrNull(Scope scope, DkxTokenCollection tokens)
        {
            if (tokens.Count == 0) throw new ArgumentException("Token collection is empty.");

            var token = tokens[0];
            if (token.Type == DkxTokenType.Keyword && DkxConst.Keywords.ControlStatementStartKeyword.Contains(token.Text))
            {
                switch (token.Text)
                {
                    case DkxConst.Keywords.If:
                        return IfStatement.Parse(scope, tokens);
                    case DkxConst.Keywords.Return:
                        return ReturnStatement.Parse(scope, tokens);
                    case DkxConst.Keywords.Var:
                        return VarStatement.Parse(scope, tokens);
                    default:
                        throw new CodeException(token.Span, ErrorCode.KeywordNotValidHere, token.Text);
                }
            }

            if (tokens.Count == 1 && tokens[0].IsStatementEnd) return new EmptyStatement(scope, tokens[0].Span);

            return TokensToExpressionStatementOrNull(scope, tokens);
        }

        private static Statement TokensToExpressionStatementOrNull(Scope scope, DkxTokenCollection tokens)
        {
            if (tokens.Count == 0) throw new ArgumentException("Token collection is empty.");

            try
            {
                var stream = new DkxTokenStream(tokens);
                if (ExpressionParser.TryReadDataType(scope, stream, out var dataType, out var dataTypeSpan))
                {
                    if (stream.Peek().IsIdentifier)
                    {
                        // This is a variable declaration.
                        var stmt = VariableDeclarationStatement.Parse(scope, dataType, dataTypeSpan, stream);
                        if (!stream.EndOfStream) scope.Report(stream.Read().Span, ErrorCode.SyntaxError);
                        return stmt;
                    }
                    else stream.Position = 0;
                }

                var exp = ExpressionParser.ReadExpressionOrNull(scope, stream);
                if (exp != null)
                {
                    var stmt = new ExpressionStatement(scope, exp);
                    if (stream.Peek().IsStatementEnd) stream.Position++;
                    if (!stream.EndOfStream) scope.Report(stream.Read().Span, ErrorCode.SyntaxError);
                    return stmt;
                }

                if (stream.Peek().IsStatementEnd)
                {
                    var stmt = new EmptyStatement(scope, stream.Read().Span);
                    if (!stream.EndOfStream) scope.Report(stream.Read().Span, ErrorCode.SyntaxError);
                    return stmt;
                }

                if (stream.EndOfStream) throw new CodeException(stream.Read().Span, ErrorCode.SyntaxError);
                return null;
            }
            catch (CodeException ex)
            {
                scope.AddReportItem(ex.ToReportItem());
                return null;
            }
        }

        public static Statement[] ReadBodyOrStatement(Scope scope, DkxTokenStream stream, Span errorSpan)
        {
            var token = stream.Peek();
            if (token.IsScope)
            {
                stream.Position++;
                return SplitTokensIntoStatements(scope, token.Tokens).ToArray();
            }
            else
            {
                var end = FindStatementEnd(stream.Tokens, stream.Position, allowControlStatements: true);
                var tokens = stream.GetRange(stream.Position, end < 0 ? -1 : end - stream.Position);
                stream.Position = end < 0 ? stream.Length : end;
                var stmt = TokensToStatementOrNull(scope, tokens);
                if (stmt == null) throw new CodeException(errorSpan, ErrorCode.ExpectedStatement);
                return new Statement[] { stmt };
            }
        }
    }
}
