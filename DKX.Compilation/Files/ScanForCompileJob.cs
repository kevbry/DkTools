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

namespace DKX.Compilation.Files
{
    public class ScanForCompileJob : ICompileJob
    {
        private DkAppContext _app;
        private string _workDir;
        private ICompileJobQueue _compileQueue;
        private ICompileFileJobFactory _compileFileJobFactory;
        private IObjectFileReaderFactory _objectFileReaderFactory;
        private ITableHashProvider _tableHashProvider;
        private Dictionary<string, DateTime> _dateCache = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public ScanForCompileJob(
            DkAppContext app,
            string workDir,
            ICompileJobQueue compileQueue,
            ICompileFileJobFactory compileFileJobFactory,
            IObjectFileReaderFactory objectFileReaderFactory,
            ITableHashProvider tableHashProvider)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _workDir = workDir ?? throw new ArgumentNullException(nameof(workDir));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _compileFileJobFactory = compileFileJobFactory ?? throw new ArgumentNullException(nameof(compileFileJobFactory));
            _objectFileReaderFactory = objectFileReaderFactory ?? throw new ArgumentNullException(nameof(objectFileReaderFactory));
            _tableHashProvider = tableHashProvider ?? throw new ArgumentNullException(nameof(tableHashProvider));
        }

        public string Description => "Scan DKX Compiles";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.DebugAsync("Scanning for compiles");

            var allDkxFiles = ScanForDkxFiles().ToList();
            var allExportFiles = _app.FileSystem.GetFilesInDirectoryRecursive(_workDir, "*" + CompileConstants.DkxExportsExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Process all the known DKX files.
            foreach (var dkxFile in allDkxFiles)
            {
                if (await ShouldCompileFileAsync(dkxFile))
                {
                    var compileFileJob = _compileFileJobFactory.CreateCompileFileJob(dkxFile.dkxPathName, dkxFile.wbdkPathName, dkxFile.objectPathName, dkxFile.fileContext);
                    await _compileQueue.EnqueueCompileJobAsync(compileFileJob);
                }
                allExportFiles.Remove(dkxFile.objectPathName);
            }

            // Everything left in allExportFiles will be just those where the DKX source file was deleted.
            foreach (var exportPathName in allExportFiles)
            {
                await _app.Log.DebugAsync("DKX deletion detected; removing object file: {0}", exportPathName);
                _app.FileSystem.DeleteFile(exportPathName);
            }
        }

        private IEnumerable<ScanResultFile> ScanForDkxFiles()
        {
            var foundFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sourceDir in _app.Settings.SourceDirs)
            {
                foreach (var result in ScanForDkxFiles(sourceDir, string.Empty))
                {
                    if (foundFiles.Contains(result.dkxPathName)) continue;
                    foundFiles.Add(result.dkxPathName);
                    yield return result;
                }
            }
        }

        private IEnumerable<ScanResultFile> ScanForDkxFiles(string sourceDir, string relPath)
        {
            foreach (var pathName in _app.FileSystem.GetFilesInDirectory(sourceDir))
            {
                var fileContext = DkxFileUtil.GetFileContext(pathName);
                if (fileContext == null) continue;

                var exportPathName = PathUtil.CombinePath(_workDir, relPath, PathUtil.GetFileName(pathName) + CompileConstants.DkxExportsExtension);

                yield return new ScanResultFile
                {
                    dkxPathName = pathName,
                    wbdkPathName = DkxFileUtil.GetWbdkPathName(pathName),
                    objectPathName = exportPathName,
                    relPath = relPath,
                    fileContext = fileContext.Value
                };
            }

            foreach (var path in _app.FileSystem.GetDirectoriesInDirectory(sourceDir))
            {
                foreach (var result in ScanForDkxFiles(path, PathUtil.CombinePath(relPath, PathUtil.GetFileName(path))))
                {
                    yield return result;
                }
            }
        }

        private struct ScanResultFile
        {
            public string dkxPathName;
            public string wbdkPathName;
            public string objectPathName;
            public string relPath;
            public FileContext fileContext;
        }

        private async Task<bool> ShouldCompileFileAsync(ScanResultFile file)
        {
            // Check if the file is newer than the export file.
            if (_app.FileSystem.FileExists(file.objectPathName))
            {
                var dkxDate = GetFileDate(file.dkxPathName);
                var objectDate = GetFileDate(file.objectPathName);
                if (dkxDate > objectDate)
                {
                    await _app.Log.DebugAsync("DKX file is newer than the object file: {0}", file.dkxPathName);
                    return true;
                }

                // Check file dependencies
                var objectFileReader = _objectFileReaderFactory.CreateObjectFileReader(file.objectPathName);
                foreach (var fileDep in objectFileReader.GetFileDependencies())
                {
                    if (_app.FileSystem.FileExists(fileDep.PathName))
                    {
                        var depDate = GetFileDate(fileDep.PathName);
                        if (dkxDate > depDate)
                        {
                            await _app.Log.DebugAsync("DKX file is newer than a file dependency: {0}, {1}", file.dkxPathName, fileDep.PathName);
                            return true;
                        }
                    }
                    else
                    {
                        await _app.Log.DebugAsync("DKX file is dependent on deleted file: {0}, {1}", file.dkxPathName, fileDep.PathName);
                        return true;
                    }
                }

                // Check table dependencies
                foreach (var tableDep in objectFileReader.GetTableDependencies())
                {
                    if (_app.Settings.Dict.IsTable(tableDep.TableName))
                    {
                        if (tableDep.Hash != _tableHashProvider.GetTableHash(tableDep.TableName))
                        {
                            await _app.Log.DebugAsync("DKX file is dependent on changed table '{1}': {0}", file.dkxPathName, tableDep.TableName);
                            return true;
                        }
                    }
                    else
                    {
                        await _app.Log.DebugAsync("DKX file is dependent on deleted table '{1}': {0}", file.dkxPathName, tableDep.TableName);
                        return true;
                    }
                }
            }
            else
            {
                await _app.Log.DebugAsync("Exports file does not yet exist: {0}", file.dkxPathName);
                return true;
            }

            return false;
        }

        private DateTime GetFileDate(string pathName)
        {
            if (_dateCache.TryGetValue(pathName, out var date)) return date;

            date = _app.FileSystem.GetFileModifiedDate(pathName);
            _dateCache[pathName] = date;
            return date;
        }
    }
}
