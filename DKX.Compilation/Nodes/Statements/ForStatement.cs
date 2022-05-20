using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Variables;
using System.Linq;

namespace DKX.Compilation.Nodes.Statements
{
    class ForStatement : Statement, IVariableScopeNode
    {
        private ForBody _initializer;
        private Chain _condition;
        private Chain _iteration;
        private ForBody _body;
        private VariableStore _variableStore;

        public ForStatement(Node parent, CodeSpan keywordSpan, NodeBodyContext bodyContext)
            : base(parent, keywordSpan)
        {
            _variableStore = new VariableStore(parent.GetContainerOrNull<IVariableScopeNode>());

            if (!Code.ReadExact('('))
            {
                ReportItem(Code.Position, ErrorCode.ExpectedToken, '(');
                return;
            }

            var forContext = new NodeBodyContext(this); // Variables defined in the initializer statement should be in this for's scope.

            Code.SkipWhiteSpace();
            _initializer = new ForBody(this, new CodeSpan(Code.Position, Code.Position));

            // Read the initializer statements
            if (DataType.TryParse(Code, out var declDataType, out var declDataTypeSpan))
            {
                _initializer.ReadSpecificVariableDeclaration(
                    bodyContext: forContext,
                    dataType: declDataType,
                    dataTypeSpan: declDataTypeSpan,
                    requireAllVariablesToBeInitialized: true,
                    stmtSpanOut: out _);
            }
            else if (!Code.ReadExact(';'))
            {
                while (true)
                {
                    if (!_initializer.ReadStatement(
                        bodyContext: forContext,
                        stmtSpanOut: out _,
                        allowControlStatements: false,
                        allowVariableDeclarations: false,
                        tryReadStatementEndToken: false)) return;
                    if (Code.ReadExact(';')) break;
                    if (Code.ReadExact(',')) continue;

                    ReportItem(Code.Position, ErrorCode.ExpectedToken, ';');
                    Code.SkipToAfterExit();
                    return;
                }
            }

            // Read the condition
            _condition = ExpressionParser.ReadExpressionOrNull(forContext);
            if (_condition == null) ReportItem(Code.Position, ErrorCode.ExpectedExpression);
            _condition?.Report(this);
            if (!Code.ReadExact(';')) ReportItem(Code.Position, ErrorCode.ExpectedToken, ';');

            // Read the iteration statements
            Code.SkipWhiteSpace();
            _iteration = ExpressionParser.ReadExpressionOrNull(forContext);
            if (!Code.ReadExact(')')) ReportItem(Code.Position, ErrorCode.ExpectedToken, ')');

            // Read the body
            Code.SkipWhiteSpace();
            _body = new ForBody(this, new CodeSpan(Code.Position, Code.Position));
            if (Code.ReadExact('{'))
            {
                if (!_body.ReadCodeBody(forContext, Code.Span.End)) return;
            }
            else
            {
                if (!_body.ReadStatement(forContext, out _)) return;
            }

            var span = keywordSpan;
            if (_initializer != null) span = span.Envelope(_initializer.Span);
            if (_condition != null) span = span.Envelope(_condition.Span);
            if (_iteration != null) span = span.Envelope(_iteration.Span);
            if (_body != null) span = span.Envelope(_body.Span);
            Span = span;
        }

        public CodeSpan BodySpan { get => Span; set => Span = value; }
        public IVariableStore VariableStore => _variableStore;

        public override void ToCode(OpCodeGenerator code, int parentOffset)
        {
            code.WriteOpCode(OpCode.For, parentOffset, Span);
            code.WriteOpen();
            _initializer?.ToCode(code, Span.Start);
            code.WriteDelim();
            _condition?.ToCode(code, Span.Start);
            code.WriteDelim();
            _iteration?.ToCode(code, Span.Start);
            code.WriteDelim();
            _body?.ToCode(code, Span.Start);
            code.WriteClose();
        }

        public override bool IsEmptyCode => false;

        private class ForBody : Statement, IBodyNode
        {
            public ForBody(Node parent, CodeSpan span)
                : base(parent, span)
            {
            }

            public override void ToCode(OpCodeGenerator code, int parentOffset)
            {
                code.WriteOpen();
                GenerateStatementsCode(code, parentOffset);
                code.WriteClose();
            }

            public override bool IsEmptyCode => Statements.All(x => x.IsEmptyCode);

            public CodeSpan BodySpan { get => Span; set => Span = value; }
        }
    }
}
