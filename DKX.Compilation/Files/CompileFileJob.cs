using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.Files
{
    public class CompileFileJob : ICompileJob
    {
        private DkAppContext _app;
        private string _dkxPathName;
        private string _targetPath;
        private IReportItemCollector _reportCollector;
        private IProject _project;

        public CompileFileJob(
            DkAppContext app,
            string dkxPathName,
            string targetPath,
            IReportItemCollector reportCollector,
            IProject project)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _targetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
            _project = project ?? throw new ArgumentNullException(nameof(project));
        }

        public string Description => $"Compile File: {_dkxPathName}";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.InfoAsync("Compiling: {0}", _dkxPathName);

            var source = await _app.FileSystem.ReadFileTextAsync(_dkxPathName);
            var cp = new DkxCodeParser(_dkxPathName, source);
            var fileScope = new FileScope(_dkxPathName, cp, ProcessingDepth.Full);
            fileScope.ProcessFile(_project);

            var reportItems = fileScope.ReportItems.ToList();
            _reportCollector.AddReportItems(reportItems);
            fileScope.ClearReportItems();
            if (reportItems.Any(e => e.Severity == ErrorSeverity.Error)) return;

            var generatedFiles = fileScope.GenerateWbdkCode(_targetPath);
            reportItems = fileScope.ReportItems.ToList();
            _reportCollector.AddReportItems(reportItems);
            if (reportItems.Any(e => e.Severity == ErrorSeverity.Error)) return;

            foreach (var generatedFile in generatedFiles)
            {
                await _app.Log.DebugAsync("Writing: {0}", generatedFile.WbdkPathName);
                _app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(generatedFile.WbdkPathName));
                _app.FileSystem.WriteFileText(generatedFile.WbdkPathName, generatedFile.Code);
            }

            _project.OnCompileCompleted(
                dkxPathName: _dkxPathName,
                fileDependencies: DkxConst.EmptyStringArray,    // TODO
                tableDependencies: TableHash.EmptyArray);       // TODO
        }
    }
}
