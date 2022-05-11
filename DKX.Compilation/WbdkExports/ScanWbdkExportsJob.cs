using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using DKX.Compilation.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private ITableHashProvider _tableHashProvider;

        public ScanWbdkExportsJob(
            DkAppContext app,
            ICompileJobQueue jobQueue,
            string workDir,
            IWbdkExportsFileReaderFactory exportsReaderFactory,
            ITableHashProvider tableHashProvider)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _queue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
            _workDir = workDir ?? throw new ArgumentNullException(nameof(workDir));
            _exportsReaderFactory = exportsReaderFactory ?? throw new ArgumentNullException(nameof(exportsReaderFactory));
            _tableHashProvider = tableHashProvider ?? throw new ArgumentNullException(nameof(tableHashProvider));
        }

        public string Description => "Scan WBDK Exports";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.DebugAsync("Scanning for WBDK exports.");

            var existingExports = GetFullListOfLinkedExportFiles().ToList();
            var allExports = _app.FileSystem.GetFilesInDirectoryRecursive(_workDir, "*" + CompileConstants.WbdkExportsExtension).ToList();

            foreach (var result in existingExports)
            {
                cancel.ThrowIfCancellationRequested();

                if (await ShouldFileBeRescannedAsync(result.pathName, result.exportsPathName))
                {
                    await _queue.EnqueueCompileJobAsync(new ScanWbdkExportFileJob(_app, result.pathName, result.exportsPathName, result.fileContext, _tableHashProvider));
                }

                var exportToRemove = allExports.Where(x => x.EqualsI(result.exportsPathName)).FirstOrDefault();
                //if (exportToRemove == null) throw new InvalidOperationException($"Processed export is not in the list of all exports: {result.exportsPathName}"); TODO: remove
                allExports.Remove(exportToRemove);
            }

            // The remaining files in allExports will be the ones where the source file was deleted.
            foreach (var exportPathName in allExports)
            {
                await _app.Log.DebugAsync("Delete Detected: {0}", exportPathName);
                _app.FileSystem.DeleteFile(exportPathName);
            }
        }

        private IEnumerable<WbdkScanResult> GetFullListOfLinkedExportFiles()
        {
            var foundFiles = new HashSet<string>();

            foreach (var sourceDir in _app.Settings.SourceDirs)
            {
                if (_app.FileSystem.DirectoryExists(sourceDir))
                {
                    foreach (var result in ScanForWbdkExportFiles(sourceDir, string.Empty))
                    {
                        var pathNameLower = result.pathName.ToLower();
                        if (!foundFiles.Contains(pathNameLower))
                        {
                            foundFiles.Add(pathNameLower);
                        }

                        yield return result;
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
                        yield return new WbdkScanResult
                        {
                            pathName = pathName,
                            relPath = relPath,
                            fileContext = fileContext,
                            exportsPathName = PathUtil.CombinePath(_workDir, relPath, PathUtil.GetFileName(pathName) + CompileConstants.WbdkExportsExtension)
                        };
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
            public string exportsPathName;
        }

        private DateTime GetFileDate(string pathName)
        {
            if (_dateCache.TryGetValue(pathName, out var date)) return date;

            date = _app.FileSystem.GetFileModifiedDate(pathName);
            _dateCache[pathName] = date;
            return date;
        }

        private async Task<bool> ShouldFileBeRescannedAsync(string sourcePathName, string exportsPathName)
        {
            // Check the modified date of the source file.
            if (_app.FileSystem.FileExists(exportsPathName))
            {
                if (GetFileDate(sourcePathName) > GetFileDate(exportsPathName))
                {
                    await _app.Log.DebugAsync("Exports file is older than its source file: {0}", sourcePathName);
                    return true;
                }
            }
            else
            {
                await _app.Log.DebugAsync("Exports file does not yet exist: {0}", sourcePathName);
                return true;
            }

            // Check if any of the include dependencies have been touched.
            var exportsReader = _exportsReaderFactory.CreateReader(exportsPathName);
            var exportsDate = GetFileDate(exportsPathName);
            foreach (var includePathName in exportsReader.GetIncludeDependencies())
            {
                if (GetFileDate(includePathName) > exportsDate)
                {
                    await _app.Log.DebugAsync("Exports file is older than include dependency: {0}, {1}", sourcePathName, includePathName);
                    return true;
                }
            }

            foreach (var td in exportsReader.GetTableDependencies())
            {
                if (_app.Settings.Dict.IsTable(td.TableName))
                {
                    var currentHash = _tableHashProvider.GetTableHash(td.TableName);
                    if (currentHash != td.Hash)
                    {
                        await _app.Log.DebugAsync("Exports file is dependent on changed table '{1}': {0}", sourcePathName, td.TableName);
                        return true;
                    }
                }
                else
                {
                    await _app.Log.DebugAsync("Exports file is dependent on deleted table '{1}': {0}", sourcePathName, td.TableName);
                    return true;
                }
            }

            //_app.Log.Debug("Exports file is up to date: {0}", sourcePathName);
            return false;
        }
    }
}
