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
        private string _dkxPathName;
        private string _wbdkPathName;
        private string _objPathName;
        private FileContext _fileContext;

        public CompileFileJob(DkAppContext app, string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
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

            var obj = new ObjectFileModel
            {
                SourcePathName = _dkxPathName,
                DestinationPathName = _wbdkPathName,
                ClassName = fileNode.ClassName,
                FileDependencies = null,    // TODO
                TableDependencies = null,   // TODO
                Methods = fileNode.Methods.Select(m => m.ToObjectFile()).ToArray()
                // TODO: properties
            };

            _app.FileSystem.WriteFileText(_objPathName, JsonConvert.SerializeObject(obj));

            return Task.CompletedTask;
        }
    }
}
