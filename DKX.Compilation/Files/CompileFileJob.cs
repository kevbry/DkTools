using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using DKX.Compilation.ReportItems;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.Files
{
    public class CompileFileJob : ICompileJob
    {
        private DkAppContext _app;
        private string _dkxPathName;
        private string _objPathName;
        private IReportItemCollector _reportCollector;

        public CompileFileJob(
            DkAppContext app,
            string dkxPathName,
            string objPathName,
            IReportItemCollector reportCollector)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
            _reportCollector = reportCollector ?? throw new ArgumentNullException(nameof(reportCollector));
        }

        public string Description => $"Compile File: {_dkxPathName}";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.InfoAsync("Compiling: {0}", _dkxPathName);

            // TODO: This needs to be replaced

            //var source = _app.FileSystem.GetFileText(_dkxPathName);
            //var code = new CodeParser(source);
            //var fileNode = new FileNode(_app, _dkxPathName, code);
            //fileNode.Parse();

            //var reportItems = fileNode.ReportItems.ToList();
            //_reportCollector.AddReportItems(reportItems);
            //if (!reportItems.Any(e => e.Severity == ErrorSeverity.Error))
            //{
            //    var methods = fileNode.Methods.Select(m => m.ToObjectFile()).ToArray();
            //    if (methods.Length == 0) methods = null;

            //    var properties = fileNode.Properties.Select(p => p.ToObjectProperty()).ToArray();
            //    if (properties.Length == 0) properties = null;

            //    var memberVariables = fileNode.VariableStore.GetVariables(includeParents: false).Select(v => v.ToObjectMemberVariable()).ToArray();
            //    if (memberVariables.Length == 0) memberVariables = null;

            //    var constants = fileNode.Constants.Select(c => c.ToObjectConstant()).ToArray();
            //    if (constants.Length == 0) constants = null;

            //    var obj = new ObjectFileModel
            //    {
            //        SourcePathName = _dkxPathName,
            //        ClassName = fileNode.ClassName,
            //        FileDependencies = null,    // TODO
            //        TableDependencies = null,   // TODO
            //        Methods = methods,
            //        Properties = properties,
            //        MemberVariables = memberVariables,
            //        Constants = constants
            //    };

            //    _app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(_objPathName));
            //    _app.FileSystem.WriteFileText(_objPathName, JsonConvert.SerializeObject(obj, Formatting.Indented));
            //}
        }
    }
}
