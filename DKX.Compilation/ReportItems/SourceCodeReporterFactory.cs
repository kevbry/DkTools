using DK.AppEnvironment;
using System;

namespace DKX.Compilation.ReportItems
{
    class SourceCodeReporterFactory : ISourceCodeReporterFactory
    {
        private DkAppContext _app;
        private IReportItemCollector _reportCollector;

        public SourceCodeReporterFactory(DkAppContext app, IReportItemCollector reportCollector)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
        }

        public ISourceCodeReporter CreateSourceCodeReporter(DkAppContext app, string sourcePathName)
        {
            return new SourceCodeReporter(_app, sourcePathName, _reportCollector);
        }
    }
}
