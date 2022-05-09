using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.Jobs;
using System;

namespace DKX.Compilation.Files
{
    class CompileFileJobFactory : ICompileFileJobFactory
    {
        private DkAppContext _app;

        public CompileFileJobFactory(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public ICompileJob CreateCompileFileJob(string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            return new CompileFileJob(_app, dkxPathName, wbdkPathName, objPathName, fileContext);
        }
    }
}
