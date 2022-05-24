namespace DKX.Compilation.ObjectFiles
{
    public interface IObjectFileReaderFactory
    {
        IObjectFileReader CreateObjectFileReader(string objectPathName);
    }
}
