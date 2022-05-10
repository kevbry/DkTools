using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using DKX.Compilation.Nodes;
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
        private ICompileJobQueue _compileQueue;
        private string _dkxPathName;
        private string _wbdkPathName;
        private string _objPathName;
        private FileContext _fileContext;

        public CompileFileJob(DkAppContext app, ICompileJobQueue compileQueue, string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _wbdkPathName = wbdkPathName ?? throw new ArgumentNullException(nameof(wbdkPathName));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
            _fileContext = fileContext;
        }

        public string Description => $"Compile File: {_dkxPathName}";

        public Task ExecuteAsync(CancellationToken cancel)
        {
            _app.Log.Info("Compiling: {0}", _dkxPathName);

            var source = _app.FileSystem.GetFileText(_dkxPathName);
            var code = new CodeParser(source);
            var fileNode = new FileNode(_dkxPathName, code);
            fileNode.Parse();

            var reportItems = fileNode.ReportItems.ToList();
            _compileQueue.AddReports(reportItems);
            if (!reportItems.Any(e => e.Severity == ErrorSeverity.Error))
            {
                var methods = fileNode.Methods.Select(m => m.ToObjectFile()).ToArray();
                if (methods.Length == 0) methods = null;

                var properties = fileNode.Properties.Select(p => p.ToObjectProperty()).ToArray();
                if (properties.Length == 0) properties = null;

                var memberVariables = fileNode.Variables.Select(v => v.ToObjectMemberVariable()).ToArray();
                if (memberVariables.Length == 0) memberVariables = null;

                var obj = new ObjectFileModel
                {
                    SourcePathName = _dkxPathName,
                    DestinationPathName = _wbdkPathName,
                    ClassName = fileNode.ClassName,
                    FileDependencies = null,    // TODO
                    TableDependencies = null,   // TODO
                    Methods = methods,
                    Properties = properties,
                    MemberVariables = memberVariables
                };

                _app.FileSystem.WriteFileText(_objPathName, JsonConvert.SerializeObject(obj));
            }

            return Task.CompletedTask;
        }
    }
}
