using DKX.Compilation.Files;
using DKX.Compilation.Jobs;

namespace DKX.Compilation.Tests.Files
{
    class TestCompileFileJobFactory : ICompileFileJobFactory
    {
        public ICompileJob CreateCompileFileJob(string dkxPathName, string relPath, string objPathName)
        {
            return new TestCompileFileJob(dkxPathName, objPathName);
        }
    }
}
