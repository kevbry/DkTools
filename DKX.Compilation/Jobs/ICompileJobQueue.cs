using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Jobs
{
    public interface ICompileJobQueue
    {
        Task EnqueueCompileJobAsync(ICompileJob job);

        void AddReport(ReportItem reportItem);

        void AddReports(IEnumerable<ReportItem> reportItems);
    }
}
