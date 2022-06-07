using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Tokens;
using System;

namespace DKX.Compilation.Scopes.Statements
{
    class ContinueStatement : Statement
    {
        private ContinueStatement(Scope parent, Span span) : base(parent, span) { }

        public override bool IsEmpty => false;

        public static ContinueStatement Parse(Scope parent, DkxTokenCollection tokens)
        {
            if (tokens.Count == 0 || !tokens[0].IsKeyword(DkxConst.Keywords.Continue)) throw new InvalidOperationException("Expected first token to be the 'continue' keyword.");
            var keywordToken = tokens[0];

            var continueStatement = new ContinueStatement(parent, keywordToken.Span);
            var stream = new DkxTokenStream(tokens, 1);

            try
            {
                var breakScope = continueStatement.GetScope<IBreakScope>();
                if (breakScope == null) throw new CodeException(keywordToken.Span, ErrorCode.NoContinueScope);

                if (!stream.Peek().IsStatementEnd) throw new CodeException(keywordToken.Span, ErrorCode.ExpectedStatementEndToken);
                stream.Read();

                if (!stream.EndOfStream) throw new CodeException(stream.Read().Span, ErrorCode.SyntaxError);
            }
            catch (CodeException ex)
            {
                continueStatement.AddReportItem(ex.ToReportItem());
            }

            return continueStatement;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow)
        {
            cw.Write(DkxConst.Keywords.Continue);
            cw.WriteStatementEnd();
            cw.WriteLine();

            flow.OnBranchEnded();
        }
    }
}
