using DK.AppEnvironment;
using DKX.Compilation.Jobs;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Files
{
    class CompileFileJobFactory : ICompileFileJobFactory
    {
        private DkAppContext _app;
        private IReportItemCollector _reportCollector;

        public CompileFileJobFactory(DkAppContext app, IReportItemCollector reportCollector)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
        }

        public ICompileJob CreateCompileFileJob(string dkxPathName, string objPathName)
        {
            return new CompileFileJob(_app, dkxPathName, objPathName, _reportCollector);
        }
    }
}
