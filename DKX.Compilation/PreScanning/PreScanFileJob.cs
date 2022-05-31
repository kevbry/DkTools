using DK.AppEnvironment;
using DKX.Compilation.Jobs;
using DKX.Compilation.Project;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.PreScanning
{
    class PreScanFileJob : ICompileJob
    {
        private DkAppContext _app;
        private IProject _project;
        private string _dkxPathName;

        public PreScanFileJob(DkAppContext app, IProject project, string dkxPathName)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
        }

        public string Description => $"Pre-Scan: {_dkxPathName}";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            var source = await _app.FileSystem.ReadFileTextAsync(_dkxPathName);
            var cp = new DkxCodeParser(_dkxPathName, source);
            var file = new FileScope(_dkxPathName, cp, ProcessingDepth.ExportsOnly);
            file.ProcessFile(_project);

            _project.OnPreScanFileComplete(_dkxPathName, file.Namespaces);
        }
    }
}
