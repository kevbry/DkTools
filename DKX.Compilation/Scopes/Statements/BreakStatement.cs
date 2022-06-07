using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Tokens;
using System;

namespace DKX.Compilation.Scopes.Statements
{
    class BreakStatement : Statement
    {
        private BreakStatement(Scope parent, Span span) : base(parent, span) { }

        public override bool IsEmpty => false;

        public static BreakStatement Parse(Scope parent, DkxTokenCollection tokens)
        {
            if (tokens.Count == 0 || !tokens[0].IsKeyword(DkxConst.Keywords.Break)) throw new InvalidOperationException("Expected first token to be the 'break' keyword.");
            var keywordToken = tokens[0];

            var breakStatement = new BreakStatement(parent, keywordToken.Span);
            var stream = new DkxTokenStream(tokens, 1);

            try
            {
                var breakScope = breakStatement.GetScope<IBreakScope>();
                if (breakScope == null) throw new CodeException(keywordToken.Span, ErrorCode.NoBreakScope);

                if (!stream.Peek().IsStatementEnd) throw new CodeException(keywordToken.Span, ErrorCode.ExpectedStatementEndToken);
                stream.Read();

                if (!stream.EndOfStream) throw new CodeException(stream.Read().Span, ErrorCode.SyntaxError);
            }
            catch (CodeException ex)
            {
                breakStatement.AddReportItem(ex.ToReportItem());
            }

            return breakStatement;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow)
        {
            cw.Write(DkxConst.Keywords.Break);
            cw.WriteStatementEnd();
            cw.WriteLine();

            flow.OnBranchEnded();
        }
    }
}
