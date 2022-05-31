using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.CodeGeneration
{
    class CodeGenerationContext
    {
        private IReportItemCollector _report;

        public CodeGenerationContext(IReportItemCollector report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public IReportItemCollector Report => _report;
    }
}
