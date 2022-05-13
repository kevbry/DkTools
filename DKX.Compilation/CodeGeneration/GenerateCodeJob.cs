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
        private string _wbdkPathName;
        private string _objPathName;
        private FileContext _fileContext;
        private IObjectFileReader _objectFileReader;
        private ISourceCodeReporter _report;

        public GenerateCodeJob(
            DkAppContext app,
            ICompileJobQueue compileQueue,
            string dkxPathName,
            string wbdkPathName,
            string objPathName,
            FileContext fileContext,
            IObjectFileReader objectFileReader,
            ISourceCodeReporter reporter)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _wbdkPathName = wbdkPathName ?? throw new ArgumentNullException(nameof(wbdkPathName));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
            _fileContext = fileContext;
            _objectFileReader = objectFileReader ?? throw new ArgumentNullException(nameof(objectFileReader));
            _report = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public string Description => $"Generate Code: {_wbdkPathName}";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.InfoAsync("Generating: {0}", _wbdkPathName);

            var gen = new CodeFileGenerator(_app, _objectFileReader.GetModel(), _report);
            var fileContent = await gen.GenerateCodeAsync();

            _app.FileSystem.WriteFileText(_wbdkPathName, fileContent);
        }
    }
}
