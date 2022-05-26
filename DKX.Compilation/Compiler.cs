using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Files;
using DKX.Compilation.ObjectFiles;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
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

            // Scan for DKX files to be compiled.
            var exportsProvider = new ExportsProvider(_app);
            var tableHashProvider = new TableHashProvider(_app);
            var queue = new CompileQueue(_app, "DKX Compilation Queue");
            await queue.EnqueueCompileJobAsync(new ScanForCompileJob(
                app: _app,
                objectDir: _objectDir,
                targetSourceDir: _targetSourceDir,
                compileQueue: queue,
                compileFileJobFactory: new CompileFileJobFactory(_app, queue, _targetSourceDir, exportsProvider),
                tableHashProvider: tableHashProvider,
                objectFileReaderFactory: new ObjectFileReaderFactory(_app)));

            await queue.ProcessQueueToCompletionAsync(cancel);

            ImportReportItems(queue.ReportItems);
            if (HasErrors)
            {
                await ReportAsync();
                return;
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
            foreach (var item in _reportItems)
            {
                switch (item.Severity)
                {
                    case ErrorSeverity.Error:
                        await _app.Log.ErrorAsync(item.ToString());
                        break;
                    case ErrorSeverity.Warning:
                        await _app.Log.WarningAsync(item.ToString());
                        break;
                }
            }
        }
    }

    class InvalidAppSettingsException : Exception
    {
        public InvalidAppSettingsException(string message) : base(message) { }
    }
}
