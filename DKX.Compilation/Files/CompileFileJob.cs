using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Jobs;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using Newtonsoft.Json;
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
        private string _relPath;
        private string _objPathName;
        private string _targetPath;
        private IReportItemCollector _reportCollector;

        public CompileFileJob(
            DkAppContext app,
            string dkxPathName,
            string relPath,
            string objPathName,
            string targetPath,
            IReportItemCollector reportCollector)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _relPath = relPath ?? throw new ArgumentNullException(nameof(relPath));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
            _targetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
        }

        public string Description => $"Compile File: {_dkxPathName}";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.InfoAsync("Compiling: {0}", _dkxPathName);

            var source = _app.FileSystem.GetFileText(_dkxPathName);
            var cp = new DkxCodeParser(source);
            var fileScope = new FileScope(_dkxPathName, cp, ProcessingDepth.Full);
            fileScope.ProcessFile();

            var reportItems = fileScope.ReportItems.ToList();
            _reportCollector.AddReportItems(reportItems);
            if (!reportItems.Any(e => e.Severity == ErrorSeverity.Error))
            {
                var objectModel = fileScope.CreateObjectModel();

                await _app.Log.DebugAsync("Writing: {0}", _objPathName);
                _app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(_objPathName));
                _app.FileSystem.WriteFileText(_objPathName, JsonConvert.SerializeObject(objectModel, Formatting.Indented));

                foreach (var fileContext in objectModel.FileContexts?.Select(x => x.Context) ?? FileContextHelper.EmptyArray)
                {
                    var wbdkPathName = DkxFileHelper.DkxPathNameToWbdkPathName(_dkxPathName, _relPath, _targetPath, fileContext);
                    var cw = new CodeWriter();
                    fileScope.GenerateWbdkCode(cw);

                    await _app.Log.DebugAsync("Writing: {0}", wbdkPathName);
                    _app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(wbdkPathName));
                    _app.FileSystem.WriteFileText(wbdkPathName, cw.Code);
                }
            }
        }
    }
}
