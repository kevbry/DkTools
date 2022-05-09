using DK.Code;
using DKX.Compilation.Files;
using DKX.Compilation.Jobs;

namespace DKX.Compilation.Tests.Files
{
    class TestCompileFileJobFactory : ICompileFileJobFactory
    {
        public ICompileJob CreateCompileFileJob(string dkxPathName, string wbdkPathName, string objPathName, FileContext fileContext)
        {
            return new TestCompileFileJob(dkxPathName, wbdkPathName, objPathName, fileContext);
        }
    }
}
