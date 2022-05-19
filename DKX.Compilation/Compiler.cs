using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Schema;
using DKX.Compilation.WbdkExports;
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
        private string _workDir;
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

            // Scan legacy files for exports
            await _app.Log.InfoAsync("Checking WBDK exports");
            var queue = new CompileQueue(_app, "WBDK Exports Scan Queue");
            var tableHashProvider = new TableHashProvider(_app);
            await queue.EnqueueCompileJobAsync(new ScanWbdkExportsJob(_app, queue, _workDir,
                new WbdkExportsFileReaderFactory(_app),
                tableHashProvider));

            await queue.ProcessQueueToCompletionAsync(cancel);

            ImportReportItems(queue.ReportItems);
            if (HasErrors)
            {
                await ReportAsync();
                return;
            }

            queue = new CompileQueue(_app, "DKX Compilation Queue");
            await queue.EnqueueCompileJobAsync(new ScanForCompileJob(
                app: _app,
                workDir: _workDir,
                compileQueue: queue,
                compileFileJobFactory: new CompileFileJobFactory(_app, queue),
                objectFileReaderFactory: new ObjectFileReaderFactory(_app),
                tableHashProvider: tableHashProvider));

            await queue.ProcessQueueToCompletionAsync(cancel);

            ImportReportItems(queue.ReportItems);
            if (HasErrors)
            {
                await ReportAsync();
                return;
            }

            queue = new CompileQueue(_app, "WBDK Code Generator Queue");
            var objectFileReaderFactory = new ObjectFileReaderFactory(_app);
            var reporterFactory = new SourceCodeReporterFactory(_app, queue);
            await queue.EnqueueCompileJobAsync(new ScanForGenerateCodeJob(
                app: _app,
                workDir: _workDir,
                compileQueue: queue,
                generateCodeJobFactory: new GenerateCodeJobFactory(_app, queue, objectFileReaderFactory, reporterFactory),
                objectFileReaderFactory: objectFileReaderFactory));

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
            _workDir = _app.Settings.ExeDirs.FirstOrDefault();
            if (_workDir == null) throw new InvalidAppSettingsException("No executable directory is configured.");
            if (!_app.FileSystem.DirectoryExists(_workDir))
            {
                await _app.Log.InfoAsync("Creating directory: {0}", _workDir);
                _app.FileSystem.CreateDirectory(_workDir);
            }
            _workDir = _app.FileSystem.CombinePath(_workDir, CompileConstants.WorkDirectoryName);
            if (!_app.FileSystem.DirectoryExists(_workDir))
            {
                await _app.Log.InfoAsync("Creating directory: {0}", _workDir);
                _app.FileSystem.CreateDirectory(_workDir);
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
