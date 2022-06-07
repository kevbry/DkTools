using DK;
using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Project;
using DKX.Compilation.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.Jobs
{
    public class ScanForCompileJob : ICompileJob
    {
        private DkAppContext _app;
        private ICompileJobQueue _compileQueue;
        private ICompileFileJobFactory _compileFileJobFactory;
        private ITableHashProvider _tableHashProvider;
        private IProject _project;
        private Dictionary<string, DateTime> _dateCache = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private CompilePhase _phase;

        public ScanForCompileJob(
            DkAppContext app,
            ICompileJobQueue compileQueue,
            ICompileFileJobFactory compileFileJobFactory,
            ITableHashProvider tableHashProvider,
            IProject project,
            CompilePhase phase)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _compileFileJobFactory = compileFileJobFactory ?? throw new ArgumentNullException(nameof(compileFileJobFactory));
            _tableHashProvider = tableHashProvider ?? throw new ArgumentNullException(nameof(tableHashProvider));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _phase = phase;
        }

        public string Description => "Scan DKX Compiles";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.DebugAsync("Scanning for compiles");

            var allDkxFiles = FindAllDkxFiles();

            foreach (var dkxPathName in allDkxFiles)
            {
                if (await ShouldCompileFileAsync(dkxPathName))
                {
                    var compileFileJob = _compileFileJobFactory.CreateCompileFileJob(dkxPathName);
                    await _compileQueue.EnqueueCompileJobAsync(compileFileJob);
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

        private async Task<bool> ShouldCompileFileAsync(string dkxPathName)
        {
            var projectTimeStamp = _project.GetCompileTimeStamp(dkxPathName);
            var fileTimeStamp = GetFileDate(dkxPathName);

            if (fileTimeStamp > projectTimeStamp)
            {
                await _app.Log.DebugAsync("DKX file has changed: {0}", dkxPathName);
                return true;
            }

            foreach (var depPathName in  _project.GetFileDependencies(dkxPathName))
            {
                if (_app.FileSystem.FileExists(depPathName))
                {
                    var depTimeStamp = GetFileDate(dkxPathName);
                    if (depTimeStamp > projectTimeStamp)
                    {
                        await _app.Log.DebugAsync("DKX file dependency has changed: {0} ({1})", dkxPathName, depPathName);
                        return true;
                    }
                }
                else
                {
                    await _app.Log.DebugAsync("DKX file dependency has been deleted: {0} ({1})", dkxPathName, depPathName);
                    return true;
                }
            }

            foreach (var tableDep in _project.GetTableDependencies(dkxPathName))
            {
                var tableName = tableDep.TableName;
                var projectHash = tableDep.Hash;
                var currentHash = _tableHashProvider.GetTableHash(tableName);
                if (projectHash != currentHash)
                {
                    await _app.Log.DebugAsync("DKX table dependency has changed: {0} ({1})", dkxPathName, tableName);
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
