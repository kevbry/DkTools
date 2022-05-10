using DK.Code;
using DKX.Compilation.Expressions;
using System;

namespace DKX.Compilation.Nodes
{
    class ReturnStatement : Statement
    {
        private Chain _exp;

        public ReturnStatement(Node parent, CodeSpan keywordSpan)
            : base(parent)
        {
            var returnNode = GetContainerOrNull<IReturnTargetNode>();
            if (returnNode == null) throw new InvalidOperationException("Return statement is not inside a container that can return a value.");

            if (!returnNode.ReturnDataType.IsVoid)
            {
                _exp = ExpressionParser.ReadExpressionOrNull(parent.Code);
                if (_exp == null) ReportItem(keywordSpan, ErrorCode.ExpectedExpression);
            }
        }

        public override string ToCode() => $"ret({_exp?.ToCode()})";
    }
}
