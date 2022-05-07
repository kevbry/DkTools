using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.WbdkExports
{
    public class ScanWbdkExportsJob : ICompileJob
    {
        private DkAppContext _app;
        private ICompileJobQueue _queue;
        private string _workDir;

        public ScanWbdkExportsJob(DkAppContext app, ICompileJobQueue jobQueue, string workDir)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _queue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
            _workDir = workDir ?? throw new ArgumentNullException(nameof(workDir));
        }

        public string Description => "Scan WBDK Exports";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            _app.Log.Debug("Scanning for WBDK exports.");

            var foundFiles = new HashSet<string>();

            foreach (var sourceDir in _app.Settings.SourceDirs)
            {
                if (_app.FileSystem.DirectoryExists(sourceDir))
                {
                    foreach (var result in ScanForWbdkExportFiles(sourceDir, string.Empty))
                    {
                        cancel.ThrowIfCancellationRequested();
                        if (foundFiles.Count > 10) break;   // TODO

                        var pathNameLower = result.pathName.ToLower();
                        if (!foundFiles.Contains(pathNameLower))
                        {
                            foundFiles.Add(pathNameLower);

                            var relDir = PathUtil.CombinePath(_workDir, result.relPath);
                            var exportsPathName = PathUtil.CombinePath(relDir, PathUtil.GetFileName(result.pathName) + CompileConstants.WbdkExportsExtension);
                            await _queue.EnqueueCompileJobAsync(new ScanWbdkExportFileJob(_app, result.pathName, exportsPathName, result.fileContext));
                        }
                    }
                }
            }
        }

        private IEnumerable<WbdkScanResult> ScanForWbdkExportFiles(string dir, string relPath)
        {
            foreach (var pathName in _app.FileSystem.GetFilesInDirectory(dir))
            {
                var fileContext = FileContextHelper.GetFileContextFromFileName(pathName);
                switch (fileContext)
                {
                    case FileContext.Function:
                    case FileContext.ClientClass:
                    case FileContext.NeutralClass:
                    case FileContext.ServerClass:
                        yield return new WbdkScanResult { pathName = pathName, relPath = relPath, fileContext = fileContext };
                        break;
                }
            }

            foreach (var path in _app.FileSystem.GetDirectoriesInDirectory(dir))
            {
                foreach (var result in ScanForWbdkExportFiles(path, PathUtil.CombinePath(relPath, PathUtil.GetFileName(path))))
                {
                    yield return result;
                }
            }
        }

        private struct WbdkScanResult
        {
            public string pathName;
            public string relPath;
            public FileContext fileContext;
        }
    }
}
