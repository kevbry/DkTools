using DK.AppEnvironment;
using DKX.Compilation.Jobs;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
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

        public CompileFileJobFactory(DkAppContext app, IReportItemCollector reportCollector, string targetPath, IProject project, CompilePhase phase)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
            _targetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _phase = phase;
        }

        public ICompileJob CreateCompileFileJob(string dkxPathName)
        {
            return new CompileFileJob(_app, dkxPathName, _targetPath, _reportCollector, _project, _phase);
        }
    }
}
