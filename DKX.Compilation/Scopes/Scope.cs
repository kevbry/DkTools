using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Tokens;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes
{
    public abstract class Scope : ISourceCodeReporter
    {
        internal abstract Task GenerateWbdkCodeAsync(CodeWriter cw);

        private Scope _parent;

        public Scope(Scope parent)
        {
            _parent = parent;
        }

        public Scope Parent => _parent;

        public T GetScope<T>() where T : class => (this as T) ?? _parent?.GetScope<T>();

        public virtual void OnReport(CodeSpan span, ErrorCode errorCode, params object[] args) => _parent.OnReport(span, errorCode, args);

        public virtual bool HasErrors => _parent.HasErrors;

        public Task ReportAsync(CodeSpan span, ErrorCode code, params object[] args)
        {
            OnReport(span, code, args);
            return Task.CompletedTask;
        }

        protected async Task ReportUnusedTokensAsync(DkxTokenCollection tokens, TokenUseTracker used)
        {
            foreach (var badToken in tokens.GetUnused(used))
            {
                await ReportAsync(badToken.Span, ErrorCode.SyntaxError);
            }
        }
    }
}
