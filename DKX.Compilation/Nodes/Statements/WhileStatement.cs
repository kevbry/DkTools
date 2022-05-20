using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Variables;

namespace DKX.Compilation.Nodes.Statements
{
    class WhileStatement : Statement, IBodyNode, IVariableScopeNode
    {
        private Chain _condition;
        private CodeSpan _bodySpan;
        private VariableStore _variableStore;

        public WhileStatement(Node parent, CodeSpan keywordSpan, NodeBodyContext bodyContext)
            : base(parent, keywordSpan)
        {
            ReadConditionAndBody(bodyContext);
            if (!BodySpan.IsEmpty) Span = keywordSpan.Envelope(_bodySpan);

            _variableStore = new VariableStore(parent?.GetContainerOrNull<IVariableScopeNode>());
        }

        public CodeSpan BodySpan { get => _bodySpan; set => _bodySpan = value; }

        private bool ReadConditionAndBody(NodeBodyContext bodyContext)
        {
            if (Code.ReadExact('('))
            {
                _condition = ExpressionParser.ReadExpressionOrNull(bodyContext);
                if (_condition == null)
                {
                    Code.ReadExact(')');
                    Code.SkipToAfterExit();
                    ReportItem(Code.Position, ErrorCode.ExpectedExpression);
                    return false;
                }
                _condition?.Report(this);

                if (!Code.ReadExact(')'))
                {
                    ReportItem(Code.Position, ErrorCode.ExpectedToken, ')');
                    Code.SkipToAfterExit();
                    return false;
                }

                if (!ReadStatementOrBody(bodyContext, bodyRequired: false)) return false;
            }
            else
            {
                _condition = ExpressionParser.ReadExpressionOrNull(bodyContext);
                if (_condition == null)
                {
                    ReportItem(Code.Position, ErrorCode.ExpectedExpression);
                    return false;
                }
                _condition?.Report(this);

                if (!Code.ReadExact('{'))
                {
                    ReportItem(Code.Position, ErrorCode.ExpectedToken, '{');
                    return false;
                }

                if (!ReadCodeBody(bodyContext, Code.Span.End)) return false;
            }

            return true;
        }

        private bool ReadStatementOrBody(NodeBodyContext bodyContext, bool bodyRequired)
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

        public override void ToCode(OpCodeGenerator code, int parentOffset)
        {
            code.WriteOpCode(OpCode.While, parentOffset, Span);
            code.WriteOpen();
            _condition?.ToCode(code, Span.Start);
            code.WriteDelim();
            code.WriteOpen();
            GenerateStatementsCode(code, Span.Start);
            code.WriteClose();  // body
            code.WriteClose();  // while
        }

        public override bool IsEmptyCode => false;

        public IVariableStore VariableStore => _variableStore;
    }
}
