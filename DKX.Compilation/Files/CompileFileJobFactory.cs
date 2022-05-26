using DK.AppEnvironment;
using DKX.Compilation.Jobs;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using System;

namespace DKX.Compilation.Files
{
    class CompileFileJobFactory : ICompileFileJobFactory
    {
        private DkAppContext _app;
        private IReportItemCollector _reportCollector;
        private string _targetPath;
        private IExportsProvider _exportsProvider;

        public CompileFileJobFactory(DkAppContext app, IReportItemCollector reportCollector, string targetPath, IExportsProvider exportsProvider)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
            _targetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
            _exportsProvider = exportsProvider ?? throw new ArgumentNullException(nameof(exportsProvider));
        }

        public ICompileJob CreateCompileFileJob(string dkxPathName, string relPath, string objPathName)
        {
            return new CompileFileJob(_app, dkxPathName, relPath, objPathName, _targetPath, _reportCollector, _exportsProvider);
        }
    }
}
