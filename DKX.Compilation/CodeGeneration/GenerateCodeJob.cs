using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.Files;
using DKX.Compilation.Jobs;
using DKX.Compilation.ReportItems;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.CodeGeneration
{
    public class GenerateCodeJob : ICompileJob
    {
        private DkAppContext _app;
        private ICompileJobQueue _compileQueue;
        private string _dkxPathName;
        private string _objPathName;
        private IObjectFileReader _objectFileReader;
        private ISourceCodeReporter _report;

        public GenerateCodeJob(
            DkAppContext app,
            ICompileJobQueue compileQueue,
            string dkxPathName,
            string objPathName,
            IObjectFileReader objectFileReader,
            ISourceCodeReporter reporter)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
            _objectFileReader = objectFileReader ?? throw new ArgumentNullException(nameof(objectFileReader));
            _report = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public string Description => $"Generate Code: {_dkxPathName}";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            var gen = new CodeFileGenerator(_app, _objectFileReader.GetModel(), _report);

            foreach (var fileContext in _objectFileReader.GetFileContexts())
            {
                var wbdkPathName = DkxFileHelper.DkxPathNameToWbdkPathName(_dkxPathName, fileContext);
                await _app.Log.InfoAsync("Generating: {0}", wbdkPathName);
                var fileContent = await gen.GenerateCodeAsync(fileContext, wbdkPathName);
                if (!_report.HasErrors) _app.FileSystem.WriteFileText(wbdkPathName, fileContent);
            }
        }
    }
}
