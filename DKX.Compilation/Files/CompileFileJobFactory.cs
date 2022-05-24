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
        private string _targetPath;

        public CompileFileJobFactory(DkAppContext app, IReportItemCollector reportCollector, string targetPath)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
            _targetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
        }

        public ICompileJob CreateCompileFileJob(string dkxPathName, string relPath, string objPathName)
        {
            return new CompileFileJob(_app, dkxPathName, relPath, objPathName, _targetPath, _reportCollector);
        }
    }
}
