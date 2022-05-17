using DK.Code;

namespace DKX.Compilation.Expressions
{
    static class ExpressionParser
    {
        public static Chain ReadExpressionOrNull(CodeParser code)
        {
            return ReadValueOrNull(code, 0);
        }

        private static Chain ReadValueOrNull(CodeParser code, OpPrec leftPrec)
        {
            if (code.ReadWord())
            {
                var chain = new IdentifierChain(code.Text, code.Span);
                return ReadAfterValue(code, chain, leftPrec);
            }
            else if (code.ReadStringLiteral())
            {
                var chain = new StringLiteralChain(CodeParser.StringLiteralToString(code.Text), code.Span);
                return ReadAfterValue(code, chain, leftPrec);
            }
            else if (code.ReadNumber())
            {
                var chain = new NumberChain(code.Text, code.Span);
                return ReadAfterValue(code, chain, leftPrec);
            }
            else if (code.ReadExact('('))
            {
                var chain = ReadValueOrNull(code, 0);
                if (!code.ReadExact(')'))
                {
                    return new ErrorChain(chain, code.Position, ErrorCode.ExpectedToken, ')');
                }
                else
                {
                    return ReadAfterValue(code, chain, leftPrec);
                }
            }
            else return null;
        }

        private static Chain ReadAfterValue(CodeParser code, Chain chain, OpPrec leftPrec)
        {
            var startPos = code.Position;
            var op = ReadOperatorOrNull(code);
            if (op != null)
            {
                var opPrec = op.Value.op.GetPrecedence();
                if (leftPrec >= opPrec)
                {
                    code.Position = startPos;
                    return chain;
                }

                if (op.Value.op.IsUnaryPost())
                {
                    var newChain = new OperatorChain(op.Value.op, chain, right: null);
                    return ReadAfterValue(code, newChain, leftPrec);
                }
                else
                {
                    var right = ReadValueOrNull(code, opPrec);
                    if (right == null)
                    {
                        code.Position = startPos;
                        return new ErrorChain(chain, chain.Span.Envelope(op.Value.span), ErrorCode.OperatorExpectsValueOnRight, op.Value.op.GetText());
                    }
                    else
                    {
                        var newChain = new OperatorChain(op.Value.op, chain, ReadAfterValue(code, right, opPrec));
                        return ReadAfterValue(code, newChain, leftPrec);
                    }
                }
            }
            else
            {
                return chain;
            }
        }

        private static OpReadResult? ReadOperatorOrNull(CodeParser code)
        {
            // Unary-pre (not, negative) operators will not be read here because this method is only called after a value.

            if (code.ReadExact('.')) return new OpReadResult(Operator.Dot, code.Span);

            if (code.ReadExact("+=")) return new OpReadResult(Operator.AssignAdd, code.Span);
            if (code.ReadExact("++")) return new OpReadResult(Operator.Increment, code.Span);
            if (code.ReadExact('+')) return new OpReadResult(Operator.Add, code.Span);

            if (code.ReadExact("-=")) return new OpReadResult(Operator.AssignSubtract, code.Span);
            if (code.ReadExact("-+")) return new OpReadResult(Operator.Decrement, code.Span);
            if (code.ReadExact('-')) return new OpReadResult(Operator.Subtract, code.Span);

            if (code.ReadExact("*=")) return new OpReadResult(Operator.AssignMultiply, code.Span);
            if (code.ReadExact('*')) return new OpReadResult(Operator.Multiply, code.Span);

            if (code.ReadExact("/=")) return new OpReadResult(Operator.AssignDivide, code.Span);
            if (code.ReadExact('/')) return new OpReadResult(Operator.Divide, code.Span);

            if (code.ReadExact("%=")) return new OpReadResult(Operator.AssignModulus, code.Span);
            if (code.ReadExact('%')) return new OpReadResult(Operator.Modulus, code.Span);

            if (code.ReadExact("==")) return new OpReadResult(Operator.Equal, code.Span);
            if (code.ReadExact('=')) return new OpReadResult(Operator.Assign, code.Span);

            if (code.ReadExact("!=")) return new OpReadResult(Operator.NotEqual, code.Span);

            if (code.ReadExact("<=")) return new OpReadResult(Operator.LessEqual, code.Span);
            if (code.ReadExact('<')) return new OpReadResult(Operator.LessThan, code.Span);

            if (code.ReadExact(">=")) return new OpReadResult(Operator.GreaterEqual, code.Span);
            if (code.ReadExact('>')) return new OpReadResult(Operator.GreaterThan, code.Span);

            return null;
        }

        private struct OpReadResult
        {
            public Operator op;
            public CodeSpan span;

            public OpReadResult(Operator op, CodeSpan span)
            {
                this.op = op;
                this.span = span;
            }
        }
    }
}
