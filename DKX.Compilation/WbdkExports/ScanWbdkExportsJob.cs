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
        private IWbdkExportsFileReaderFactory _exportsReaderFactory;
        private Dictionary<string, DateTime> _dateCache = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public ScanWbdkExportsJob(DkAppContext app, ICompileJobQueue jobQueue, string workDir, IWbdkExportsFileReaderFactory exportsReaderFactory)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _queue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
            _workDir = workDir ?? throw new ArgumentNullException(nameof(workDir));
            _exportsReaderFactory = exportsReaderFactory ?? throw new ArgumentNullException(nameof(exportsReaderFactory));
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

                        var pathNameLower = result.pathName.ToLower();
                        if (!foundFiles.Contains(pathNameLower))
                        {
                            foundFiles.Add(pathNameLower);

                            var relDir = PathUtil.CombinePath(_workDir, result.relPath);
                            var exportsPathName = PathUtil.CombinePath(relDir, PathUtil.GetFileName(result.pathName) + CompileConstants.WbdkExportsExtension);

                            if (ShouldFileBeRescanned(result.pathName, exportsPathName))
                            {
                                await _queue.EnqueueCompileJobAsync(new ScanWbdkExportFileJob(_app, result.pathName, exportsPathName, result.fileContext));
                            }
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

        private DateTime GetFileDate(string pathName)
        {
            if (_dateCache.TryGetValue(pathName, out var date)) return date;

            date = _app.FileSystem.GetFileModifiedDate(pathName);
            _dateCache[pathName] = date;
            return date;
        }

        private bool ShouldFileBeRescanned(string sourcePathName, string exportsPathName)
        {
            // Check the modified date of the source file.
            if (_app.FileSystem.FileExists(exportsPathName))
            {
                if (GetFileDate(sourcePathName) > GetFileDate(exportsPathName))
                {
                    _app.Log.Debug("Exports file is older than it's source file: {0}", sourcePathName);
                    return true;
                }
            }
            else
            {
                _app.Log.Debug("Exports file does not yet exist: {0}", sourcePathName);
                return true;
            }

            // Check if any of the include dependencies have been touched.
            var exportsReader = _exportsReaderFactory.CreateReader(exportsPathName);
            var exportsDate = GetFileDate(exportsPathName);
            foreach (var includePathName in exportsReader.GetIncludeDependencies())
            {
                if (GetFileDate(includePathName) > exportsDate)
                {
                    _app.Log.Debug("Exports file is older than include dependency: {0}, {1}", sourcePathName, includePathName);
                    return true;
                }
            }

            //_app.Log.Debug("Exports file is up to date: {0}", sourcePathName);
            return false;
        }
    }
}
