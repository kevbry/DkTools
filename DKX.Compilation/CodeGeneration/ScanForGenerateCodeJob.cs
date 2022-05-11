using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.Files;
using DKX.Compilation.Jobs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.CodeGeneration
{
    class ScanForGenerateCodeJob : ICompileJob
    {
        private DkAppContext _app;
        private string _workDir;
        private ICompileJobQueue _compileQueue;
        private IObjectFileReaderFactory _objectFileReaderFactory;
        private IGenerateCodeJobFactory _generateCodeJobFactory;
        private Dictionary<string, DateTime> _dateCache = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public ScanForGenerateCodeJob(DkAppContext app, string workDir, ICompileJobQueue compileQueue,
            IObjectFileReaderFactory objectFileReaderFactory, IGenerateCodeJobFactory generateCodeJobFactory)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _workDir = workDir ?? throw new ArgumentNullException(nameof(workDir));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _objectFileReaderFactory = objectFileReaderFactory ?? throw new ArgumentNullException(nameof(objectFileReaderFactory));
            _generateCodeJobFactory = generateCodeJobFactory ?? throw new ArgumentNullException(nameof(generateCodeJobFactory));
        }

        public string Description => "Scan WBDK Code Generation";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.DebugAsync("Scanning for code generation");

            var allObjectFiles = await ScanForObjectFilesAsync();

            // Process all the known object files.
            foreach (var objFile in allObjectFiles)
            {
                if (!_app.FileSystem.FileExists(objFile.dkxPathName))
                {
                    await _app.Log.DebugAsync("DKX deletion detected; removing WBDK and object files: {0}, {1}", objFile.wbdkPathName, objFile.objectPathName);
                    //_app.FileSystem.DeleteFile(objFile.wbdkPathName);
                    //_app.FileSystem.DeleteFile(objFile.objectPathName);
                }

                if (await ShouldProcessFileAsync(objFile))
                {
                    var genFileJob = _generateCodeJobFactory.CreateGenerateCodeJob(objFile.dkxPathName, objFile.wbdkPathName, objFile.objectPathName, objFile.fileContext);
                    await _compileQueue.EnqueueCompileJobAsync(genFileJob);
                }
            }
        }

        private async Task<List<ScanResultFile>> ScanForObjectFilesAsync()
        {
            var foundFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<ScanResultFile>();

            if (_app.FileSystem.DirectoryExists(_workDir))
            {
                foreach (var result in await ScanForObjectFilesAsync(_workDir, string.Empty))
                {
                    if (foundFiles.Contains(result.dkxPathName)) continue;
                    foundFiles.Add(result.dkxPathName);
                    results.Add(result);
                }
            }

            return results;
        }

        private async Task<List<ScanResultFile>> ScanForObjectFilesAsync(string sourceDir, string relPath)
        {
            var results = new List<ScanResultFile>();

            foreach (var pathName in _app.FileSystem.GetFilesInDirectory(sourceDir, "*" + CompileConstants.DkxExportsExtension))
            {
                var objectFileReader = _objectFileReaderFactory.CreateObjectFileReader(pathName);

                var dkxPathName = objectFileReader.GetDkxPathName();
                if (string.IsNullOrEmpty(dkxPathName))
                {
                    await _app.Log.WarningAsync("Object file has no DKX path name: {0}", pathName);
                    continue;
                }

                var wbdkPathName = objectFileReader.GetWbdkPathName();
                if (string.IsNullOrEmpty(wbdkPathName))
                {
                    await _app.Log.WarningAsync("Object file has no WBDK path name: {0}", pathName);
                    continue;
                }

                var fileContext = DkxFileUtil.GetFileContext(dkxPathName);
                if (fileContext == null) continue;

                results.Add(new ScanResultFile
                {
                    dkxPathName = dkxPathName,
                    wbdkPathName = wbdkPathName,
                    objectPathName = pathName,
                    relPath = relPath,
                    fileContext = fileContext.Value
                });
            }

            foreach (var path in _app.FileSystem.GetDirectoriesInDirectory(sourceDir))
            {
                results.AddRange(await ScanForObjectFilesAsync(path, PathUtil.CombinePath(relPath, PathUtil.GetFileName(path))));
            }

            return results;
        }

        private struct ScanResultFile
        {
            public string dkxPathName;
            public string wbdkPathName;
            public string objectPathName;
            public string relPath;
            public FileContext fileContext;
        }

        private async Task<bool> ShouldProcessFileAsync(ScanResultFile file)
        {
            // Check if the object file is newer than the WBDK file.
            if (_app.FileSystem.FileExists(file.wbdkPathName))
            {
                var objectDate = GetFileDate(file.objectPathName);
                var wbdkDate = GetFileDate(file.wbdkPathName);
                if (objectDate > wbdkDate)
                {
                    await _app.Log.DebugAsync("DKX object file is newer than the WBDK file: {0}", file.wbdkPathName);
                    return true;
                }
            }
            else
            {
                await _app.Log.DebugAsync("WBDK file does not yet exist: {0}", file.wbdkPathName);
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
