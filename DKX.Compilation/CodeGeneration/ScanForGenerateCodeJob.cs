using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.Files;
using DKX.Compilation.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
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
                if (!_app.FileSystem.FileExists(objFile.DkxPathName))
                {
                    foreach (var fileContext in objFile.FileContexts)
                    {
                        var wbdkPathName = DkxFileHelper.DkxPathNameToWbdkPathName(objFile.DkxPathName, fileContext);
                        if (_app.FileSystem.FileExists(wbdkPathName))
                        {
                            await _app.Log.DebugAsync("DKX deletion detected; removing WBDK file(s): {0}", wbdkPathName);
                            _app.FileSystem.DeleteFile(wbdkPathName);
                        }
                    }

                    await _app.Log.DebugAsync("DKX deletion detected; removing object file: {0}", objFile.ObjectPathName);
                    _app.FileSystem.DeleteFile(objFile.ObjectPathName);
                }
                else
                {
                    // Check for file contexts no longer included in the source.
                    // For example, where changing a trigger from neutral to server would no longer produce a .nc file.
                    var remainingContexts = new List<FileContext>(DkxFileHelper.ApplicableFileContexts);
                    foreach (var fileContext in objFile.ObjectFileReader.GetFileContexts()) remainingContexts.Remove(fileContext);
                    foreach (var fileContext in remainingContexts)
                    {
                        var wbdkPathName = DkxFileHelper.DkxPathNameToWbdkPathName(objFile.DkxPathName, fileContext);
                        if (_app.FileSystem.FileExists(wbdkPathName))
                        {
                            await _app.Log.DebugAsync("DKX deletion detected; removing WBDK file(s): {0}", wbdkPathName);
                            _app.FileSystem.DeleteFile(wbdkPathName);
                        }
                    }
                }

                if (await ShouldProcessFileAsync(objFile))
                {
                    var genFileJob = _generateCodeJobFactory.CreateGenerateCodeJob(objFile.DkxPathName, objFile.ObjectPathName);
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
                    if (foundFiles.Contains(result.DkxPathName)) continue;
                    foundFiles.Add(result.DkxPathName);
                    results.Add(result);
                }
            }

            return results;
        }

        private async Task<List<ScanResultFile>> ScanForObjectFilesAsync(string sourceDir, string relPath)
        {
            var results = new List<ScanResultFile>();

            foreach (var pathName in _app.FileSystem.GetFilesInDirectory(sourceDir, "*" + DkxConst.DkxObjectExtension))
            {
                var objectFileReader = _objectFileReaderFactory.CreateObjectFileReader(pathName);

                var dkxPathName = objectFileReader.GetDkxPathName();
                if (string.IsNullOrEmpty(dkxPathName))
                {
                    await _app.Log.WarningAsync("Object file has no DKX path name: {0}", pathName);
                    continue;
                }

                results.Add(new ScanResultFile(dkxPathName, pathName, relPath, objectFileReader.GetFileContexts(), objectFileReader));
            }

            foreach (var path in _app.FileSystem.GetDirectoriesInDirectory(sourceDir))
            {
                results.AddRange(await ScanForObjectFilesAsync(path, PathUtil.CombinePath(relPath, PathUtil.GetFileName(path))));
            }

            return results;
        }

        private class ScanResultFile
        {
            public string DkxPathName { get; private set; }
            public string ObjectPathName { get; private set; }
            public string RelPath { get; private set; }
            public List<FileContext> FileContexts { get; private set; }
            public IObjectFileReader ObjectFileReader { get; private set; }

            public ScanResultFile(string dkxPathName, string objPathName, string relPath, IEnumerable<FileContext> fileContexts, IObjectFileReader objectFileReader)
            {
                DkxPathName = dkxPathName;
                ObjectPathName = objPathName;
                RelPath = relPath;
                FileContexts = fileContexts.ToList();
                ObjectFileReader = objectFileReader;
            }
        }

        private async Task<bool> ShouldProcessFileAsync(ScanResultFile file)
        {
            // Check if the object file is newer than the WBDK file.
            foreach (var fileContext in file.FileContexts)
            {
                var wbdkPathName = DkxFileHelper.DkxPathNameToWbdkPathName(file.DkxPathName, fileContext);

                if (_app.FileSystem.FileExists(wbdkPathName))
                {
                    var objectDate = GetFileDate(file.ObjectPathName);
                    var wbdkDate = GetFileDate(wbdkPathName);
                    if (objectDate > wbdkDate)
                    {
                        await _app.Log.DebugAsync("DKX object file is newer than the WBDK file: {0}", wbdkPathName);
                        return true;
                    }
                }
                else
                {
                    await _app.Log.DebugAsync("WBDK file does not yet exist: {0}", wbdkPathName);
                    return true;
                }
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
