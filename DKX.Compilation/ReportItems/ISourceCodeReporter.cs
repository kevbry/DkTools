using DK.Code;

namespace DKX.Compilation.ReportItems
{
    public interface ISourceCodeReporter
    {
        void ReportItem(int pos, ErrorCode code, params object[] args);

        void ReportItem(CodeSpan span, ErrorCode code, params object[] args);
    }
}
