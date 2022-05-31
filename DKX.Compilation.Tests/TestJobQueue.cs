using DKX.Compilation.Jobs;
using DKX.Compilation.ReportItems;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    class TestJobQueue : ICompileJobQueue, IReportItemCollector
    {
        public List<ICompileJob> Jobs { get; set; } = new List<ICompileJob>();
        public List<ReportItem> ReportItems { get; set; } = new List<ReportItem>();

        public Task EnqueueCompileJobAsync(ICompileJob job)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }

        public void AddReportItem(ReportItem reportItem) => ReportItems.Add(reportItem);

        public void AddReportItems(IEnumerable<ReportItem> reportItems) => ReportItems.AddRange(reportItems);

        public void Report(Span span, ErrorCode errorCode, params object[] args) => ReportItems.Add(new ReportItem(span, errorCode, args));

        public bool HasErrors => ReportItems?.Any(x => x.Severity == ErrorSeverity.Error) ?? false;
    }
}
