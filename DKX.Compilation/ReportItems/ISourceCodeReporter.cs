using DK.Code;
using System.Threading.Tasks;

namespace DKX.Compilation.ReportItems
{
    public interface ISourceCodeReporter
    {
        Task ReportAsync(CodeSpan span, ErrorCode code, params object[] args);

        bool HasErrors { get; }
    }
}
