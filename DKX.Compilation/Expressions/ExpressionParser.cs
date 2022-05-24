using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;

namespace DKX.Compilation.Expressions
{
    static class ExpressionParser
    {
        public static Chain ReadExpressionOrNull(Scope scope, DkxTokenStream stream)
        {
            var chain = ReadValueOrNull(scope, stream, 0);
            if (chain == null) return null;
            return chain;
        }

        private static Chain ReadValueOrNull(Scope scope, DkxTokenStream stream, OpPrec leftPrec)
        {
            var startPos = stream.Position;
            var token = stream.Read();
            if (token.Type == DkxTokenType.Keyword)
            {
                Chain chain;

                switch (token.Text)
                {
                    case DkxConst.Keywords.False:
                        chain = new BooleanLiteralChain(false, token.Span);
                        return ReadAfterValue(scope, stream, chain, leftPrec);
                    case DkxConst.Keywords.True:
                        chain = new BooleanLiteralChain(true, token.Span);
                        return ReadAfterValue(scope, stream, chain, leftPrec);
                    default:
                        scope.ReportItem(token.Span, ErrorCode.SyntaxError);
                        return new ErrorChain(innerChainOrNull: null, token.Span);
                }
            }
            else if (token.IsIdentifier)
            {
                Chain chain;

                var variableStore = scope.GetScope<IVariableScope>()?.VariableStore;
                if (variableStore != null && variableStore.TryGetVariable(token.Text, includeParents: true, out var variable))
                {
                    chain = new VariableChain(variable, token.Span);
                    return ReadAfterValue(scope, stream, chain, leftPrec);
                }

                var constantStore = scope.GetScope<IConstantScope>()?.ConstantStore;
                if (constantStore != null && constantStore.TryGetConstant(token.Text, out var constant))
                {
                    chain = new ConstantChain(constant, token.Span);
                    return ReadAfterValue(scope, stream, chain, leftPrec);
                }

                scope.ReportItem(token.Span, ErrorCode.UnknownIdentifier, token.Text);
                return new ErrorChain(innerChainOrNull: null, token.Span);
            }
            else if (token.IsString)
            {
                if (token.HasError) scope.ReportItem(token.Span, ErrorCode.InvalidStringLiteral);
                var chain = new StringLiteralChain(token.Text, token.Span);
                return ReadAfterValue(scope, stream, chain, leftPrec);
            }
            else if (token.IsChar)
            {
                if (token.HasError) scope.ReportItem(token.Span, ErrorCode.InvalidCharLiteral);
                var chain = new CharLiteralChain(token.Char, token.Span);
                return ReadAfterValue(scope, stream, chain, leftPrec);
            }
            else if (token.IsNumber)
            {
                var chain = new NumericLiteralChain(token.Number, token.DataType, token.Span);
                return ReadAfterValue(scope, stream, chain, leftPrec);
            }
            else if (token.IsBrackets)
            {
                var subStream = new DkxTokenStream(token.Tokens);
                var chain = ReadValueOrNull(scope, subStream, 0);
                if (chain == null)
                {
                    scope.ReportItem(token.Span, ErrorCode.ExpectedExpression);
                    return new ErrorChain(null, token.Span);
                }
                return ReadAfterValue(scope, stream, chain, leftPrec);
            }
            else
            {
                stream.Position = startPos;
                return null;
            }
        }

        private static Chain ReadAfterValue(Scope scope, DkxTokenStream stream, Chain chain, OpPrec leftPrec)
        {
            var startPos = stream.Position;
            var token = stream.Read();
            if (token.Type == DkxTokenType.Operator)
            {
                var op = token.Operator;
                var opPrec = op.GetPrecedence();
                if (leftPrec >= opPrec)
                {
                    // The left expression takes priority over the new operator,
                    // so don't consume the right operator at this time.
                    stream.Position = startPos;
                    return chain;
                }

                if (op.IsUnaryPost())
                {
                    var newChain = new OperatorChain(op, chain, right: null);
                    return ReadAfterValue(scope, stream, newChain, leftPrec);
                }
                else
                {
                    var right = ReadValueOrNull(scope, stream, opPrec);
                    if (right == null)
                    {
                        // Was unable to read a value to the right of the operator, so stop the expression before this operator.
                        stream.Position = startPos;
                        scope.ReportItem(chain.Span.Envelope(token.Span), ErrorCode.OperatorExpectsValueOnRight, op.GetText());
                        return chain;
                    }
                    else
                    {
                        var newChain = new OperatorChain(op, chain, right);
                        return ReadAfterValue(scope, stream, newChain, leftPrec);
                    }
                }
            }
            else
            {
                stream.Position = startPos;
                return chain;
            }
        }
    }
}
