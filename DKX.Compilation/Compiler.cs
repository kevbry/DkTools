using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Files;
using DKX.Compilation.PreScanning;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Schema;
using Force.Crc32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation
{
    public class Compiler
    {
        private DkAppContext _app;
        private string _objectDir;
        private string _targetSourceDir;
        private List<ReportItem> _reportItems = new List<ReportItem>();
        private bool _haltErrors;

        public Compiler(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public IEnumerable<ReportItem> ReportItems => _reportItems;

        public async Task CompileAsync(CancellationToken cancel)
        {
            await DetermineWorkDirAsync();

            var projectPathName = PathUtil.CombinePath(_objectDir, DkxConst.ProjectFileName);
            var project = await DkxProject.CreateAsync(_app, projectPathName);

            try
            {
                //// Scan legacy files for exports
                //await _app.Log.InfoAsync("Checking WBDK exports");
                //var queue = new CompileQueue(_app, "WBDK Exports Scan Queue");
                //var tableHashProvider = new TableHashProvider(_app);
                //await queue.EnqueueCompileJobAsync(new ScanWbdkExportsJob(_app, queue, _objectDir,
                //    new WbdkExportsFileReaderFactory(_app),
                //    tableHashProvider));

                //await queue.ProcessQueueToCompletionAsync(cancel);

                //ImportReportItems(queue.ReportItems);
                //if (HasErrors)
                //{
                //    await ReportAsync();
                //    return;
                //}

                // Scan DKX files for exports.
                var queue = new CompileQueue(_app, "DKX Pre-Scan Queue");
                project.OnPreScanStarted();
                await queue.EnqueueCompileJobAsync(new PreScanJob(
                    app: _app,
                    project: project,
                    queue: queue));

                await queue.ProcessQueueToCompletionAsync(cancel);
                project.OnPreScanCompleted(queue);

                ImportReportItems(queue.ReportItems);
                if (HasErrors)
                {
                    await ReportAsync();
                    return;
                }

                // Scan for DKX files to be compiled.
                queue = new CompileQueue(_app, "DKX Compilation Queue");
                var tableHashProvider = new TableHashProvider(_app);
                await queue.EnqueueCompileJobAsync(new ScanForCompileJob(
                    app: _app,
                    compileQueue: queue,
                    compileFileJobFactory: new CompileFileJobFactory(_app, queue, _targetSourceDir, project),
                    tableHashProvider: tableHashProvider,
                    project: project));

                await queue.ProcessQueueToCompletionAsync(cancel);

                ImportReportItems(queue.ReportItems);
                if (HasErrors)
                {
                    await ReportAsync();
                    return;
                }
            }
            finally
            {
                await project.SaveAsync();
            }
        }

        private async Task DetermineWorkDirAsync()
        {
            // Object Dir
            _objectDir = _app.Settings.ExeDirs.FirstOrDefault();
            if (_objectDir == null) throw new InvalidAppSettingsException("No executable directory is configured.");
            _objectDir = PathUtil.CombinePath(_objectDir, DkxConst.WorkDirectoryName);

            if (!_app.FileSystem.DirectoryExists(_objectDir))
            {
                await _app.Log.InfoAsync("Creating object directory: {0}", _objectDir);
                _app.FileSystem.CreateDirectoryRecursive(_objectDir);
            }

            // Target Source Dir
            _targetSourceDir = null;
            foreach (var sourceDir in _app.Settings.SourceDirs)
            {
                if (sourceDir.EndsWith(DkxConst.WorkDirectoryName, StringComparison.OrdinalIgnoreCase))
                {
                    _targetSourceDir = sourceDir;
                    break;
                }
            }

            if (_targetSourceDir == null) throw new InvalidAppSettingsException($"There must be a source directory configured ending with '{DkxConst.WorkDirectoryName}'.");

            if (!_app.FileSystem.DirectoryExists(_targetSourceDir))
            {
                await _app.Log.InfoAsync("Creating WBDK target directory: {0}", _targetSourceDir);
                _app.FileSystem.CreateDirectoryRecursive(_targetSourceDir);
            }
        }

        private void ImportReportItems(IEnumerable<ReportItem> reportItems)
        {
            foreach (var item in reportItems)
            {
                if (item.Severity == ErrorSeverity.Error) _haltErrors = true;
                _reportItems.Add(item);
            }
        }

        public bool HasErrors => _haltErrors;

        public async Task ReportAsync()
        {
            var sourceCodeCache = new SourceCodeCache(_app);

            foreach (var item in _reportItems)
            {
                switch (item.Severity)
                {
                    case ErrorSeverity.Error:
                        await _app.Log.ErrorAsync(await item.ToDisplayStringAsync(sourceCodeCache));
                        break;
                    case ErrorSeverity.Warning:
                        await _app.Log.WarningAsync(await item.ToDisplayStringAsync(sourceCodeCache));
                        break;
                }
            }
        }

        public static uint ComputeHash(string data) => Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(data));

        public static string GetWbdkClassName(string fullClassName) => string.Concat(DkxConst.ClassHashPrefix, ComputeHash(fullClassName));
    }

    class InvalidAppSettingsException : Exception
    {
        public InvalidAppSettingsException(string message) : base(message) { }
    }
}
