using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.Expressions;

namespace DKX.Compilation.Nodes.Statements
{
    class IfStatement : Statement
    {
        public IfStatement(Node parent, CodeSpan keywordSpan, NodeBodyContext bodyContext)
            : base(parent, keywordSpan)
        {
            if (!ReadIfConditionAndBody(bodyContext)) return;
            while (true)
            {
                if (!Code.ReadExactWholeWord("else")) break;

                if (Code.ReadExactWholeWord("if"))
                {
                    if (!ReadIfConditionAndBody(bodyContext)) return;
                }
                else
                {
                    var elseBody = new IfBody(this, condition: null, new CodeSpan(Code.Position, Code.Position));
                    var elseBodyContext = new NodeBodyContext(elseBody);
                    if (!elseBody.ReadStatementOrBody(elseBodyContext, bodyRequired: false)) return;
                }
            }

            var span = Span;
            foreach (var node in ChildNodes)
            {
                if (node is IfBody body)
                {
                    span = span.Envelope(body.Span);
                }
            }
            Span = span;
        }

        public override bool IsEmptyCode => false;

        private bool ReadIfConditionAndBody(NodeBodyContext bodyContext)
        {
            if (Code.ReadExact('('))
            {
                var condition = ExpressionParser.ReadExpressionOrNull(bodyContext);
                if (condition == null)
                {
                    Code.ReadExact(')');
                    Code.SkipToAfterExit();
                    ReportItem(Code.Position, ErrorCode.ExpectedExpression);
                    return false;
                }

                if (!Code.ReadExact(')'))
                {
                    ReportItem(Code.Position, ErrorCode.ExpectedToken, ')');
                    Code.SkipToAfterExit();
                    return false;
                }

                var trueBody = new IfBody(this, condition, new CodeSpan(Code.Position, Code.Position));
                var trueBodyContext = new NodeBodyContext(trueBody);
                if (!trueBody.ReadStatementOrBody(trueBodyContext, bodyRequired: false)) return false;
            }
            else
            {
                var condition = ExpressionParser.ReadExpressionOrNull(bodyContext);
                if (condition == null)
                {
                    ReportItem(Code.Position, ErrorCode.ExpectedExpression);
                    return false;
                }

                if (!Code.ReadExact('{'))
                {
                    ReportItem(Code.Position, ErrorCode.ExpectedToken, '{');
                    return false;
                }

                var trueBody = new IfBody(this, condition, Code.Span);
                var trueBodyContext = new NodeBodyContext(trueBody);
                if (!trueBody.ReadCodeBody(trueBodyContext, Code.Span.End)) return false;
            }

            return true;
        }

        public override void ToCode(OpCodeGenerator code, int offset)
        {
            code.WriteOpCode(OpCode.If, offset, Span);
            code.WriteOpen();

            var first = true;
            foreach (var node in ChildNodes)
            {
                if (node is IfBody body)
                {
                    if (first) first = false;
                    else code.WriteDelim();

                    if (body.Condition != null) body.Condition.ToCode(code, Span.Start);
                    code.WriteDelim();
                    code.WriteOpen();
                    body.ToCode(code, Span.Start);
                    code.WriteClose();
                }
            }

            code.WriteClose();
        }

        private class IfBody : Statement, IBodyNode
        {
            private Chain _condition;

            public IfBody(Node parent, Chain condition, CodeSpan span)
                : base(parent, span)
            {
                _condition = condition;
                _condition?.Report(this);
            }

            public Chain Condition => _condition;

            public bool ReadStatementOrBody(NodeBodyContext bodyContext, bool bodyRequired)
            {
                if (Code.ReadExact('{'))
                {
                    if (!ReadCodeBody(bodyContext, bodyStartPos: Code.Span.End)) return false;
                    return true;
                }
                else
                {
                    if (bodyRequired)
                    {
                        ReportItem(Code.Position, ErrorCode.ExpectedToken, '{');
                        return false;
                    }

                    if (!ReadStatement(bodyContext, out var stmtSpan)) return false;

                    BodySpan = stmtSpan;
                }

                return true;
            }

            public override void ToCode(OpCodeGenerator code, int parentOffset) => GenerateStatementsCode(code, parentOffset);

            public CodeSpan BodySpan { get => Span; set => Span = value; }

            public override bool IsEmptyCode => false;
        }
    }
}
