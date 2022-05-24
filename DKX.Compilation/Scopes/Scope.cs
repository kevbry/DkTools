using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Tokens;

namespace DKX.Compilation.Scopes
{
    public abstract class Scope : ISourceCodeReporter
    {
        internal abstract void GenerateWbdkCode(CodeWriter cw);

        private Scope _parent;

        public Scope(Scope parent)
        {
            _parent = parent;
        }

        public Scope Parent => _parent;

        public T GetScope<T>() where T : class => (this as T) ?? _parent?.GetScope<T>();

        public virtual void OnReport(CodeSpan span, ErrorCode errorCode, params object[] args) => _parent.OnReport(span, errorCode, args);

        public virtual bool HasErrors => _parent.HasErrors;

        public void ReportItem(int pos, ErrorCode code, params object[] args)
        {
            OnReport(new CodeSpan(pos, pos), code, args);
        }

        public void ReportItem(CodeSpan span, ErrorCode code, params object[] args)
        {
            OnReport(span, code, args);
        }

        protected void ReportUnusedTokens(DkxTokenCollection tokens, TokenUseTracker used)
        {
            foreach (var badToken in tokens.GetUnused(used)) ReportItem(badToken.Span, ErrorCode.SyntaxError);
        }
    }
}
