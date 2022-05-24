using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using DKX.Compilation.ObjectFiles;
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
        private string _objectDir;
        private string _targetSourceDir;
        private ICompileJobQueue _compileQueue;
        private ICompileFileJobFactory _compileFileJobFactory;
        private ITableHashProvider _tableHashProvider;
        private IObjectFileReaderFactory _objectFileReaderFactory;
        private Dictionary<string, DateTime> _dateCache = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public ScanForCompileJob(
            DkAppContext app,
            string objectDir,
            string targetSourceDir,
            ICompileJobQueue compileQueue,
            ICompileFileJobFactory compileFileJobFactory,
            ITableHashProvider tableHashProvider,
            IObjectFileReaderFactory objectFileReaderFactory)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _objectDir = objectDir ?? throw new ArgumentNullException(nameof(objectDir));
            _targetSourceDir = targetSourceDir ?? throw new ArgumentNullException(nameof(targetSourceDir));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _compileFileJobFactory = compileFileJobFactory ?? throw new ArgumentNullException(nameof(compileFileJobFactory));
            _tableHashProvider = tableHashProvider ?? throw new ArgumentNullException(nameof(tableHashProvider));
            _objectFileReaderFactory = objectFileReaderFactory ?? throw new ArgumentNullException(nameof(objectFileReaderFactory));
        }

        public string Description => "Scan DKX Compiles";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.DebugAsync("Scanning for compiles");

            var allDkxFiles = ScanForDkxFiles().ToList();
            var allExportFiles = _app.FileSystem.GetFilesInDirectoryRecursive(_objectDir, "*" + DkxConst.DkxObjectExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Process all the known DKX files.
            foreach (var file in allDkxFiles)
            {
                if (await ShouldCompileFileAsync(file))
                {
                    var compileFileJob = _compileFileJobFactory.CreateCompileFileJob(file.DkxPathName, file.RelPath, file.ObjectPathName);
                    await _compileQueue.EnqueueCompileJobAsync(compileFileJob);
                }
                allExportFiles.Remove(file.ObjectPathName);
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
                    if (foundFiles.Contains(result.DkxPathName)) continue;
                    foundFiles.Add(result.DkxPathName);
                    yield return result;
                }
            }
        }

        private IEnumerable<ScanResultFile> ScanForDkxFiles(string sourceDir, string relPath)
        {
            foreach (var dkxPathName in _app.FileSystem.GetFilesInDirectory(sourceDir, "*" + DkxConst.DkxExtension))
            {
                var objPathName = PathUtil.CombinePath(_objectDir, relPath, PathUtil.GetFileNameWithoutExtension(dkxPathName) + DkxConst.DkxObjectExtension);
                yield return new ScanResultFile(dkxPathName, relPath, objPathName);
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
            public string DkxPathName { get; private set; }
            public string RelPath { get; private set; }
            public string ObjectPathName { get; private set; }

            public ScanResultFile(string dkxPathName, string relPath, string objectPathName)
            {
                DkxPathName = dkxPathName;
                RelPath = relPath;
                ObjectPathName = objectPathName;
            }
        }

        private async Task<bool> ShouldCompileFileAsync(ScanResultFile file)
        {
            // Check if the file is newer than any existing WBDK file.
            if (_app.FileSystem.FileExists(file.ObjectPathName))
            {
                var dkxDate = GetFileDate(file.DkxPathName);
                var objectDate = GetFileDate(file.ObjectPathName);
                if (dkxDate > objectDate)
                {
                    await _app.Log.DebugAsync("DKX file is newer than the object file: {0}", file.DkxPathName);
                    return true;
                }

                // Check file dependencies
                var objectFileReader = _objectFileReaderFactory.CreateObjectFileReader(file.ObjectPathName);
                foreach (var fileDep in objectFileReader.GetFileDependencies())
                {
                    if (_app.FileSystem.FileExists(fileDep))
                    {
                        var depDate = GetFileDate(fileDep);
                        if (dkxDate > depDate)
                        {
                            await _app.Log.DebugAsync("DKX file is newer than a file dependency: {0}, {1}", file.DkxPathName, fileDep);
                            return true;
                        }
                    }
                    else
                    {
                        await _app.Log.DebugAsync("DKX file is dependent on deleted file: {0}, {1}", file.DkxPathName, fileDep);
                        return true;
                    }
                }

                // Check table dependencies
                foreach (var tableDep in objectFileReader.GetTableDependencies())
                {
                    if (_app.Settings.Dict.IsTable(tableDep.Key))
                    {
                        if (tableDep.Value != _tableHashProvider.GetTableHash(tableDep.Key))
                        {
                            await _app.Log.DebugAsync("DKX file is dependent on changed table '{1}': {0}", file.DkxPathName, tableDep.Key);
                            return true;
                        }
                    }
                    else
                    {
                        await _app.Log.DebugAsync("DKX file is dependent on deleted table '{1}': {0}", file.DkxPathName, tableDep.Key);
                        return true;
                    }
                }
            }
            else
            {
                await _app.Log.DebugAsync("Exports file does not yet exist: {0}", file.DkxPathName);
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
