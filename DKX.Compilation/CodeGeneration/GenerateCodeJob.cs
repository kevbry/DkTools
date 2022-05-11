using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DKX.Compilation.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.CodeGeneration
{
    class GenerateCodeJob : ICompileJob
    {
        private DkAppContext _app;
        private ICompileJobQueue _compileQueue;
        private string _dkxPathName;
        private string _wbdkPathName;
        private string _objPathName;
        private FileContext _fileContext;

        public GenerateCodeJob(DkAppContext app, ICompileJobQueue compileQueue, string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
            _wbdkPathName = wbdkPathName ?? throw new ArgumentNullException(nameof(wbdkPathName));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
            _fileContext = fileContext;
        }

        public string Description => $"Generate Code: {_wbdkPathName}";

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            await _app.Log.InfoAsync("Generating: {0}", _wbdkPathName);
        }
    }
}
