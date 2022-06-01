using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes.Statements
{
    class IfStatement : Statement
    {
        private List<IfCase> _cases = new List<IfCase>();

        private IfStatement(Scope parent, Span keywordSpan) : base(parent, keywordSpan) { }

        public override bool IsEmpty => false;

        public static IfStatement Parse(Scope parent, Span keywordSpan, DkxTokenStream stream)
        {
            var ifStatement = new IfStatement(parent, keywordSpan);

            var first = true;
            while (true)
            {
                var conditionToken = stream.Peek();
                if (!conditionToken.IsBrackets)
                {
                    ifStatement.Report(conditionToken.Span, ErrorCode.ExpectedCondition);
                    return ifStatement;
                }
                var condition = ExpressionParser.TryReadExpression(ifStatement, conditionToken.Tokens.ToStream());
                if (condition == null) ifStatement.Report(conditionToken.Span, ErrorCode.ExpectedCondition);
                stream.Position++;

                var ifCase = new IfCase(ifStatement, condition?.Span ?? keywordSpan, condition, first);
                first = false;
                ifCase.Statements = StatementParser.ReadBodyOrExpression(ifCase, stream, conditionToken.Span);
                ifStatement._cases.Add(ifCase);

                if (stream.Test(t => t.IsKeyword(DkxConst.Keywords.Else)))
                {
                    var elseToken = stream.Read();
                    if (stream.Test(t => t.IsKeyword(DkxConst.Keywords.If)))
                    {
                        keywordSpan = stream.Read().Span;
                        continue;
                    }
                    else
                    {
                        var elseCase = new IfCase(ifStatement, elseToken.Span, null, first: false);
                        elseCase.Statements = StatementParser.ReadBodyOrExpression(elseCase, stream, elseToken.Span);
                        ifStatement._cases.Add(elseCase);
                    }
                }
            }
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            foreach (var ifCase in _cases)
            {
                ifCase.GenerateWbdkCode(context, cw);
            }
        }

        private class IfCase : Statement, IVariableScope
        {
            private Chain _condition;   // May be null if the condition could not be parsed or it's the 'else' case
            private Statement[] _statements;    // May be null if no statements could be read
            private bool _first;
            private VariableStore _variableStore;

            public IfCase(Scope parent, Span span, Chain condition, bool first)
                : base(parent, span)
            {
                _condition = condition;
                _first = true;
                _variableStore = new VariableStore(parent?.GetScope<IVariableScope>());
            }

            public Statement[] Statements { get => _statements; set => _statements = value ?? throw new ArgumentNullException(); }
            public IVariableStore VariableStore => _variableStore;

            public override bool IsEmpty => false;

            internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
            {
                if (!_first)
                {
                    cw.Write(DkxConst.Keywords.Else);
                }
                if (_condition != null)
                {
                    if (!_first) cw.Write(' ');
                    cw.Write(DkxConst.Keywords.If);
                    cw.Write(" (");
                    var conditionFrag = _condition.ToWbdkCode_Read(context);
                    cw.Write(conditionFrag);
                    ConversionValidator.CheckConversion(DataType.Bool, conditionFrag, this);
                    cw.Write(')');
                }
                using (cw.Indent())
                {
                    foreach (var stmt in _statements ?? Statement.EmptyArray)
                    {
                        stmt.GenerateWbdkCode(context, cw);
                    }

                    this.GenerateScopeEndingWbdkCode(cw);
                }
            }
        }
    }
}
