using DK.AppEnvironment;
using DK.Diagnostics;
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

        public async Task CompileAsync(CancellationToken cancel)
        {
            DetermineWorkDir();

            // Scan legacy files for exports
            _app.Log.Info("Checking WBDK exports");
            var scanQueue = new CompileQueue(_app, "WBDK Exports Scan Queue");
            await scanQueue.EnqueueCompileJobAsync(new ScanWbdkExportsJob(_app, scanQueue, _workDir, new WbdkExportsFileReaderFactory(_app)));
            await scanQueue.ProcessQueueToCompletionAsync(cancel);
            ImportReportItems(scanQueue.ReportItems);
            if (HasErrors)
            {
                Report();
                return;
            }
        }

        private void DetermineWorkDir()
        {
            _workDir = _app.Settings.ExeDirs.FirstOrDefault();
            if (_workDir == null) throw new InvalidAppSettingsException("No executable directory is configured.");
            if (!_app.FileSystem.DirectoryExists(_workDir))
            {
                _app.Log.Info("Creating directory: {0}", _workDir);
                _app.FileSystem.CreateDirectory(_workDir);
            }
            _workDir = _app.FileSystem.CombinePath(_workDir, CompileConstants.WorkDirectoryName);
            if (!_app.FileSystem.DirectoryExists(_workDir))
            {
                _app.Log.Info("Creating directory: {0}", _workDir);
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

        public void Report()
        {
            foreach (var item in _reportItems)
            {
                switch (item.Severity)
                {
                    case ErrorSeverity.Error:
                        _app.Log.Error(item.ToString());
                        break;
                    case ErrorSeverity.Warning:
                        _app.Log.Warning(item.ToString());
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
