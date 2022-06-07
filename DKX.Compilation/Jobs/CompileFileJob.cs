using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Schema;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.Jobs
{
    public class CompileFileJob : ICompileJob
    {
        private DkAppContext _app;
        private string _dkxPathName;
        private string _targetPath;
        private IReportItemCollector _reportCollector;
        private IProject _project;
        private CompilePhase _phase;
        private ITableHashProvider _tableHashProvider;

        public CompileFileJob(
            DkAppContext app,
            string dkxPathName,
            string targetPath,
            IReportItemCollector reportCollector,
            IProject project,
            CompilePhase phase,
            ITableHashProvider tableHashProvider)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _targetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _phase = phase;
            _tableHashProvider = tableHashProvider ?? throw new ArgumentNullException(nameof(tableHashProvider));
        }

        public string Description => $"Compile File: {_dkxPathName}";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.InfoAsync("Compiling: {0}", _dkxPathName);

            var source = await _app.FileSystem.ReadFileTextAsync(_dkxPathName);
            var cp = new DkxCodeParser(_dkxPathName, source);
            var resolver = new GlobalResolver(_project);
            var fileScope = new FileScope(_dkxPathName, cp, _phase, resolver, _project);
            fileScope.ProcessFile();

            var reportItems = fileScope.ReportItems.ToList();
            _reportCollector.AddReportItems(reportItems);
            fileScope.ClearReportItems();
            if (reportItems.Any(e => e.Severity == ErrorSeverity.Error)) return;

            GeneratedCodeResult genResult = null;
            if (_phase == CompilePhase.FullCompilation)
            {
                genResult = fileScope.GenerateWbdkCode(_targetPath);
                reportItems = fileScope.ReportItems.ToList();
                _reportCollector.AddReportItems(reportItems);
                if (reportItems.Any(e => e.Severity == ErrorSeverity.Error)) return;

                foreach (var generatedFile in genResult.GeneratedFiles)
                {
                    await _app.Log.DebugAsync("Writing: {0}", generatedFile.WbdkPathName);
                    _app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(generatedFile.WbdkPathName));
                    _app.FileSystem.WriteFileText(generatedFile.WbdkPathName, generatedFile.Code);
                }
            }

            await _project.OnFileScanCompletedAsync(_phase, _dkxPathName, fileScope.Namespaces);

            if (_phase == CompilePhase.FullCompilation)
            {
                _project.OnCompileCompleted(
                    dkxPathName: _dkxPathName,
                    fileDependencies: genResult.FileDependencies,
                    tableDependencies: genResult.TableDependencies.Select(x => new TableHash(x, _tableHashProvider.GetTableHash(x))).ToList());
            }
        }
    }
}
