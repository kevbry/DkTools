using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.Expressions;
using System;

namespace DKX.Compilation.Nodes
{
    class ReturnStatement : Statement
    {
        private Chain _exp;

        public ReturnStatement(Node parent, CodeSpan keywordSpan, NodeBodyContext bodyContext)
            : base(parent, keywordSpan)
        {
            var returnNode = GetContainerOrNull<IReturnTargetNode>();
            if (returnNode == null) throw new InvalidOperationException("Return statement is not inside a container that can return a value.");

            if (!returnNode.ReturnDataType.IsVoid)
            {
                _exp = ExpressionParser.ReadExpressionOrNull(bodyContext);
                if (_exp == null) ReportItem(keywordSpan, ErrorCode.ExpectedExpression);
                else Span = keywordSpan.Envelope(_exp.Span);
            }

            if (!Code.ReadExact(';')) ReportItem(Code.Position, ErrorCode.ExpectedToken, ';');
        }

        public override string ToCode(int parentOffset)
        {
            return string.Concat(
                OpCodeGenerator.GenerateOpCode("ret", parentOffset, Span),
                "(",
                _exp?.ToCode(Span.Start),
                ")");
        }
    }
}
