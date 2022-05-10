using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.Jobs;
using System;

namespace DKX.Compilation.Files
{
    class CompileFileJobFactory : ICompileFileJobFactory
    {
        private DkAppContext _app;
        private ICompileJobQueue _compileQueue;

        public CompileFileJobFactory(DkAppContext app, ICompileJobQueue compileQueue)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _compileQueue = compileQueue ?? throw new ArgumentNullException(nameof(compileQueue));
        }

        public ICompileJob CreateCompileFileJob(string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            return new CompileFileJob(_app, _compileQueue, dkxPathName, wbdkPathName, objPathName, fileContext);
        }
    }
}
