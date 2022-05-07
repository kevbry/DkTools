namespace DKX.Compilation.WbdkExports
{
    public interface IWbdkExportsFileReaderFactory
    {
        IWbdkExportsFileReader CreateReader(string exportsPathName);
    }
}
