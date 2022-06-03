namespace DKX.Compilation.Jobs
{
    public interface ICompileFileJobFactory
    {
        ICompileJob CreateCompileFileJob(string dkxPathName);
    }
}
