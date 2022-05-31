using DK;
using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using DKX.Compilation.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.PreScanning
{
    class PreScanJob : ICompileJob
    {
        private DkAppContext _app;
        private IProject _project;
        private ICompileJobQueue _queue;

        public PreScanJob(DkAppContext app, IProject project, ICompileJobQueue queue)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public string Description => "Pre-Scan Project";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            var allDkxFiles = FindAllDkxFiles();

            await _app.Log.DebugAsync("Pre-Scanning DKX files.");

            foreach (var dkxPathName in allDkxFiles)
            {
                var scanTime = _project.GetScanTimeStamp(dkxPathName);
                var fileTime = _app.FileSystem.GetFileModifiedDate(dkxPathName);
                if (fileTime > scanTime)
                {
                    await _queue.EnqueueCompileJobAsync(new PreScanFileJob(_app, _project, dkxPathName));
                }
            }
        }

        private List<string> FindAllDkxFiles()
        {
            var files = new List<string>();

            foreach (var sourceDir in _app.Settings.SourceDirs)
            {
                if (string.IsNullOrEmpty(sourceDir)) continue;

                foreach (var pathName in _app.FileSystem.GetFilesInDirectoryRecursive(sourceDir, "*" + DkxConst.DkxExtension))
                {
                    if (!files.Any(x => x.EqualsI(pathName))) files.Add(pathName);
                }
            }

            return files;
        }
    }
}
