using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.Jobs;
using System;

namespace DKX.Compilation.CodeGeneration
{
    class GenerateCodeJobFactory : IGenerateCodeJobFactory
    {
        private DkAppContext _app;
        private ICompileJobQueue _compileQueue;

        public GenerateCodeJobFactory(DkAppContext app, ICompileJobQueue compileQueue)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
        }

        public ICompileJob CreateGenerateCodeJob(string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            return new GenerateCodeJob(_app, _compileQueue, dkxPathName, wbdkPathName, objPathName, fileContext);
        }
    }
}
