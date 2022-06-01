using DK.AppEnvironment;
using DKX.Compilation.Jobs;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Schema;
using System;

namespace DKX.Compilation.Files
{
    class CompileFileJobFactory : ICompileFileJobFactory
    {
        private DkAppContext _app;
        private IReportItemCollector _reportCollector;
        private string _targetPath;
        private IProject _project;
        private CompilePhase _phase;
        private ITableHashProvider _tableHashProvider;

        public CompileFileJobFactory(DkAppContext app, IReportItemCollector reportCollector, string targetPath, IProject project, CompilePhase phase, ITableHashProvider tableHashProvider)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
            _targetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _phase = phase;
            _tableHashProvider = tableHashProvider ?? throw new ArgumentNullException(nameof(tableHashProvider));
        }

        public ICompileJob CreateCompileFileJob(string dkxPathName)
        {
            return new CompileFileJob(_app, dkxPathName, _targetPath, _reportCollector, _project, _phase, _tableHashProvider);
        }
    }
}
