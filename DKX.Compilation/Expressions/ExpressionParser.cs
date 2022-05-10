using DK.Code;
using System;

namespace DKX.Compilation.Expressions
{
    static class ExpressionParser
    {
        public static Chain ReadExpressionOrNull(CodeParser code)
        {
            return ReadChainOrNull(code);
        }

        private static Chain ReadChainOrNull(CodeParser code)
        {
            var chain = ReadValueOrNull(code);
            if (chain == null) return null;

            var precedence = 0;
            do
            {
                precedence = ReadAfterValue(code, ref chain, precedence);
            }
            while (precedence != 0);

            return chain;
        }

        private static Chain ReadValueOrNull(CodeParser code)
        {
            if (code.ReadWord())
            {
                return new IdentifierChain(code.Text, code.Span);
            }
            else if (code.ReadStringLiteral())
            {
                return new StringLiteralChain(CodeParser.StringLiteralToString(code.Text), code.Span);
            }
            else if (code.ReadNumber())
            {
                return new NumberChain(code.Text, code.Span);
            }
            else if (code.ReadExact('('))
            {
                var chain = ReadChainOrNull(code);
                if (!code.ReadExact(')'))
                {
                    return new ErrorChain(chain, code.Position, ErrorCode.ExpectedToken, ')');
                }
                else
                {
                    return chain;
                }
            }
            else return null;
        }

        private static int ReadAfterValue(CodeParser code, ref Chain chain, int leftPrec)
        {
            var startPos = code.Position;
            var op = ReadOperatorOrNull(code);
            if (op != null)
            {
                var opPrec = op.Value.op.GetPrecedence();
                if (leftPrec <= opPrec)
                {
                    var right = ReadValueOrNull(code);
                    if (right == null)
                    {
                        code.Position = startPos;
                        chain = new ErrorChain(chain, chain.Span.Envelope(op.Value.span), ErrorCode.OperatorExpectsValueOnRight, op.Value.op.GetText());
                        return 0;
                    }
                    else
                    {
                        chain = new OperatorChain(op.Value.op, chain, right);
                        return opPrec;
                    }
                }
                else
                {
                    code.Position = startPos;
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        private static OpReadResult? ReadOperatorOrNull(CodeParser code)
        {
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

    class CodeException : Exception
    {
        private CodeSpan _span;
        private ErrorCode _errorCode;
        private object[] _args;

        public CodeException(CodeSpan span, ErrorCode errorCode, params object[] args)
        {
            _span = span;
            _errorCode = errorCode;
            _args = args;
        }

        public CodeException(int pos, ErrorCode errorCode, params object[] args)
        {
            _span = new CodeSpan(pos, pos);
            _errorCode = errorCode;
            _args = args;
        }

        public CodeSpan Span => _span;
        public ErrorCode ErrorCode => _errorCode;
        public object[] Arguments => _args;
    }
}
