using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Tokens;
using System;

namespace DKX.Compilation.Scopes.Statements
{
    class WhileStatement : Statement
    {
        private Chain _condition;
        private Statement[] _body;

        private WhileStatement(Scope parent, Span span)
            : base(parent, span)
        { }

        public override bool IsEmpty => false;

        public static WhileStatement Parse(Scope parent, DkxTokenCollection tokens)
        {
            if (tokens.Count == 0 || !tokens[0].IsKeyword(DkxConst.Keywords.While)) throw new InvalidOperationException("Expected the first token to be the 'while' keyword.");
            var keywordToken = tokens[0];

            var whileStatement = new WhileStatement(parent, keywordToken.Span);
            var stream = new DkxTokenStream(tokens, 1);

            try
            {
                if (!stream.Peek().IsBrackets) throw new CodeException(whileStatement.Span, ErrorCode.ExpectedCondition);
                var conditionToken = stream.Read();
                whileStatement._condition = ExpressionParser.TokensToExpression(whileStatement, conditionToken.Tokens, conditionToken.Span);
                whileStatement._body = StatementParser.ReadBodyOrStatement(whileStatement, stream, keywordToken.Span);
            }
            catch (CodeException ex)
            {
                whileStatement.AddReportItem(ex.ToReportItem());
            }

            return whileStatement;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow)
        {
            var whileContext = new CodeGenerationContext(context);

            cw.Write(DkxConst.Keywords.While);
            cw.WriteSpace();
            var conditionFrag = _condition.ToWbdkCode_Read(whileContext, flow);
            if (conditionFrag.DataType.BaseType != BaseType.Bool) throw new CodeException(conditionFrag.SourceSpan, ErrorCode.ExpressionMustBeBool);
            cw.Write(conditionFrag);
            cw.WriteLine();

            var whileFlow = new FlowTrace(flow);
            using (cw.Indent())
            {
                foreach (var stmt in _body ?? Statement.EmptyArray)
                {
                    stmt.GenerateWbdkCode(context, cw, whileFlow);
                    if (!whileFlow.IsEnded) context.AfterStatementGenerated(cw);
                }

                GenerateScopeEnding(context, cw, whileFlow, methodEnding: false, Span);
            }

            flow.MergeBranches(new FlowTrace[] { whileFlow, new FlowTrace(flow) });
        }
    }
}
