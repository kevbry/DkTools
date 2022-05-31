using DKX.Compilation.CodeGeneration;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Tokens;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    public abstract class Scope : IReportItemCollector
    {
        internal abstract void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw);

        private Scope _parent;

        public Scope(Scope parent)
        {
            _parent = parent;
        }

        public Scope Parent => _parent;

        public T GetScope<T>() where T : class => (this as T) ?? _parent?.GetScope<T>();

        public virtual void OnReport(ReportItem reportItem) => _parent.OnReport(reportItem);

        public virtual bool HasErrors => _parent.HasErrors;

        public void AddReportItem(ReportItem reportItem) => OnReport(reportItem);

        public void AddReportItems(IEnumerable<ReportItem> reportItems)
        {
            foreach (var reportItem in reportItems) OnReport(reportItem);
        }

        public void Report(Span span, ErrorCode code, params object[] args) => OnReport(new ReportItem(span, code, args));

        protected void ReportUnusedTokens(DkxTokenCollection tokens, TokenUseTracker used)
        {
            foreach (var badToken in tokens.GetUnused(used))
            {
                Report(badToken.Span, ErrorCode.SyntaxError);
            }
        }
    }
}
