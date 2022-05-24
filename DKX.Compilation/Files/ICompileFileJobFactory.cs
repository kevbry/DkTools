using DKX.Compilation.Jobs;

namespace DKX.Compilation.Files
{
    public interface ICompileFileJobFactory
    {
        ICompileJob CreateCompileFileJob(string dkxPathName, string relPath, string objPathName);
    }
}
