using System.Collections.Generic;

namespace DKX.Compilation.ReportItems
{
    public interface IReportItemCollector
    {
        void AddReportItem(ReportItem reportItem);

        void AddReportItems(IEnumerable<ReportItem> reportItems);

        bool HasErrors { get; }
    }
}
