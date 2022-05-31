using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.ReportItems
{
    class ReportItemCollector : IReportItemCollector
    {
        private List<ReportItem> _items;

        public void AddReportItem(ReportItem reportItem)
        {
            if (_items == null) _items = new List<ReportItem>();
            _items.Add(reportItem);
        }

        public void AddReportItems(IEnumerable<ReportItem> reportItems)
        {
            if (_items.Any())
            {
                if (_items == null) _items = new List<ReportItem>();
                _items.AddRange(reportItems);
            }
        }

        public void Report(Span span, ErrorCode errorCode, params object[] args)
        {
            AddReportItem(new ReportItem(span, errorCode, args));
        }

        public bool HasErrors => _items?.Any(x => x.Severity == ErrorSeverity.Error) ?? false;

        public void ReportTo(IReportItemCollector ric)
        {
            if (_items != null) ric.AddReportItems(_items);
        }
    }
}
