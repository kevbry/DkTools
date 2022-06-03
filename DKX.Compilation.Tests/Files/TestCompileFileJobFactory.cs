using DKX.Compilation.Jobs;

namespace DKX.Compilation.Tests.Files
{
    class TestCompileFileJobFactory : ICompileFileJobFactory
    {
        public ICompileJob CreateCompileFileJob(string dkxPathName)
        {
            return new TestCompileFileJob(dkxPathName);
        }
    }
}
