namespace DKX.Compilation.Files
{
    public interface IObjectFileReaderFactory
    {
        IObjectFileReader CreateObjectFileReader(string objectPathName);
    }
}
