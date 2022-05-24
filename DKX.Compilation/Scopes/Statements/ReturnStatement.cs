using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Tokens;
using System;

namespace DKX.Compilation.Scopes.Statements
{
    class ReturnStatement : Statement
    {
        private DataType _dataType;
        private Chain _exp;

        public ReturnStatement(Scope parent, CodeSpan keywordSpan, DkxTokenStream stream)
            : base(parent, keywordSpan)
        {
            var returnScope = GetScope<IReturnScope>();
            if (returnScope == null) throw new InvalidOperationException("Could not get return scope.");
            _dataType = returnScope.ReturnDataType;

            if (_dataType.IsVoid)
            {
                if (!stream.Peek().IsStatementEnd) ReportItem(keywordSpan, ErrorCode.ExpectedToken, ';');
                else stream.Position++;
            }
            else
            {
                _exp = ExpressionParser.ReadExpressionOrNull(this, stream);
                if (_exp == null) ReportItem(keywordSpan, ErrorCode.ExpectedExpression);
            }
        }

        public override bool IsEmpty => false;

        internal override void GenerateWbdkCode(CodeWriter cw)
        {
            cw.Write("return");
            if (_exp != null)
            {
                cw.Write(' ');
                cw.Write(_exp.ToWbdkCode_Read(this));
            }
            cw.Write(';');
            cw.WriteLine();
        }
    }
}
