using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Variables.ConstTerms
{
    class ConstResolutionContext
    {
        private IReportItemCollector _report;
        private IProject _project;

        public ConstResolutionContext(IReportItemCollector report, IProject project)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _project = project ?? throw new ArgumentNullException(nameof(project));
        }

        public IProject Project => _project;
        public IReportItemCollector Report => _report;
    }
}
