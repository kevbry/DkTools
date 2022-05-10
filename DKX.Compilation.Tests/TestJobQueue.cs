using DKX.Compilation.Jobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    class TestJobQueue : ICompileJobQueue
    {
        public List<ICompileJob> Jobs { get; set; } = new List<ICompileJob>();
        public List<ReportItem> ReportItems { get; set; } = new List<ReportItem>();

        public Task EnqueueCompileJobAsync(ICompileJob job)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }

        public void AddReport(ReportItem reportItem) => ReportItems.Add(reportItem);

        public void AddReports(IEnumerable<ReportItem> reportItems) => ReportItems.AddRange(reportItems);
    }
}
