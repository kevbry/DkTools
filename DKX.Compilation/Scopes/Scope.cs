using DK.Code;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Scopes
{
    public abstract class Scope : ISourceCodeReporter
    {
        private Scope _parent;

        public Scope(Scope parent)
        {
            _parent = parent;
        }

        public Scope Parent => _parent;

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
    }
}
