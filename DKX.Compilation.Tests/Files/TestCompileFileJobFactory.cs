using DK.Code;
using DKX.Compilation.Files;
using DKX.Compilation.Jobs;

namespace DKX.Compilation.Tests.Files
{
    class TestCompileFileJobFactory : ICompileFileJobFactory
    {
        public ICompileJob CreateCompileFileJob(string dkxPathName, string objPathName)
        {
            return new TestCompileFileJob(dkxPathName, objPathName);
        }
    }
}
