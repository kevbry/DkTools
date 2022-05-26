using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.ReportItems
{
    /// <summary>
    /// Implementation of a source code reporter where the original source is not available and
    /// must be retrieved by reading the file from the file system.
    /// </summary>
    class SourceCodeReporter : ISourceCodeReporter
    {
        private DkAppContext _app;
        private string _sourcePathName;
        private string _source;
        private IReportItemCollector _reportCollector;

        public SourceCodeReporter(DkAppContext app, string sourcePathName, IReportItemCollector reportCollector)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _sourcePathName = sourcePathName ?? throw new ArgumentNullException(nameof(sourcePathName));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
        }

        private async Task EnsureFileLoadedAsync()
        {
            if (_source == null)
            {
                try
                {
                    _source = await _app.FileSystem.ReadFileTextAsync(_sourcePathName);
                }
                catch (Exception ex)
                {
                    _app.Log.Error(ex, "Error when attempting to load DKX source code from '{0}'.", _sourcePathName);
                    _source = string.Empty;
                }
            }
        }

        public async Task AddReportAsync(int pos, ErrorCode code, params object[] args)
        {
            await EnsureFileLoadedAsync();
            _reportCollector.AddReportItem(new ReportItem(_sourcePathName, _source, pos, code, args));
        }

        public async Task ReportAsync(CodeSpan span, ErrorCode code, params object[] args)
        {
            await EnsureFileLoadedAsync();
            _reportCollector.AddReportItem(new ReportItem(_sourcePathName, _source, span, code, args));
        }

        public bool HasErrors => _reportCollector.HasErrors;
    }
}
