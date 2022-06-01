using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.CodeGeneration
{
    class CodeGenerationContext
    {
        private IReportItemCollector _report;
        private IProject _project;

        public CodeGenerationContext(IReportItemCollector report, IProject project)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _project = project ?? throw new ArgumentNullException(nameof(project));
        }

        public IProject Project => _project;
        public IReportItemCollector Report => _report;
    }
}
