using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Tokens;
using DKX.Compilation.Validation;
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

        public static IfStatement Parse(Scope parent, DkxTokenCollection tokens)
        {
            if (tokens.Count == 0 || !tokens[0].IsKeyword(DkxConst.Keywords.If)) throw new InvalidOperationException("Expected the first token to be the 'if' keyword.");
            var keywordToken = tokens[0];

            var ifStatement = new IfStatement(parent, keywordToken.Span);
            var stream = new DkxTokenStream(tokens, 1);

            try
            {
                var first = true;
                while (true)
                {
                    var conditionToken = stream.Read();
                    if (!conditionToken.IsBrackets) throw new CodeException(conditionToken.Span, ErrorCode.ExpectedCondition);
                    var condition = ExpressionParser.TokensToExpressionStatement(ifStatement, conditionToken.Tokens, keywordToken.Span);

                    var ifCase = new IfCase(ifStatement, condition?.Span ?? keywordToken.Span, condition, first, finalElse: false);
                    first = false;
                    ifCase.Statements = StatementParser.ReadBodyOrStatement(ifCase, stream, conditionToken.Span);
                    ifStatement._cases.Add(ifCase);

                    if (stream.Test(t => t.IsKeyword(DkxConst.Keywords.Else)))
                    {
                        var elseToken = stream.Read();
                        if (stream.Test(t => t.IsKeyword(DkxConst.Keywords.If)))
                        {
                            keywordToken = stream.Read();
                            continue;
                        }
                        else
                        {
                            var elseCase = new IfCase(ifStatement, elseToken.Span, null, first: false, finalElse: true);
                            elseCase.Statements = StatementParser.ReadBodyOrStatement(elseCase, stream, elseToken.Span);
                            ifStatement._cases.Add(elseCase);
                            break;
                        }
                    }
                    else break;
                }
            }
            catch (CodeException ex)
            {
                ifStatement.AddReportItem(ex.ToReportItem());
            }

            return ifStatement;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow)
        {
            var flows = new List<FlowTrace>();
            var gotFinalElse = false;

            foreach (var ifCase in _cases)
            {
                var caseFlow = new FlowTrace(flow);
                flows.Add(caseFlow);
                ifCase.GenerateWbdkCode(context, cw, caseFlow);

                if (ifCase.IsFinalElse) gotFinalElse = true;
            }

            if (!gotFinalElse) flows.Add(new FlowTrace(flow));

            flow.MergeBranches(flows);
        }

        private class IfCase : Statement, IVariableScope
        {
            private Chain _condition;   // May be null if the condition could not be parsed or it's the 'else' case
            private Statement[] _statements;    // May be null if no statements could be read
            private bool _first;
            private VariableStore _variableStore;
            private bool _finalElse;

            public IfCase(Scope parent, Span span, Chain condition, bool first, bool finalElse)
                : base(parent, span)
            {
                _condition = condition;
                _first = first;
                _variableStore = new VariableStore(parent.GetScope<IVariableScope>());
                _finalElse = finalElse;
            }

            public bool IsFinalElse => _finalElse;
            public Statement[] Statements { get => _statements; set => _statements = value ?? throw new ArgumentNullException(); }
            public IVariableStore VariableStore => _variableStore;

            public override bool IsEmpty => false;

            internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow)
            {
                if (!_first)
                {
                    cw.Write(DkxConst.Keywords.Else);
                }
                if (_condition != null)
                {
                    if (!_first) cw.Write(' ');
                    cw.Write(DkxConst.Keywords.If);
                    cw.Write(' ');
                    var conditionFrag = _condition.ToWbdkCode_Read(context, flow);
                    cw.Write(conditionFrag);
                    ConversionValidator.CheckConversion(DataType.Bool, conditionFrag, this);
                }
                cw.WriteLine();
                using (cw.Indent())
                {
                    foreach (var stmt in _statements ?? Statement.EmptyArray)
                    {
                        stmt.GenerateWbdkCode(context, cw, flow);
                    }

                    GenerateScopeEnding(context, cw, flow, methodEnding: false, Span);
                }
            }
        }
    }
}
