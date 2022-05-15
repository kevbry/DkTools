using DKX.Compilation.Jobs;

namespace DKX.Compilation.CodeGeneration
{
    interface IGenerateCodeJobFactory
    {
        ICompileJob CreateGenerateCodeJob(string dkxPathName, string objPathName);
    }
}
