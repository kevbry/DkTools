using System.Collections.Generic;

namespace DKX.Compilation.Files
{
    public interface IObjectFileReader
    {
        ObjectFileModel GetModel();

        string GetWbdkPathName();

        string GetDkxPathName();

        IEnumerable<ObjectFileDependency> GetFileDependencies();

        IEnumerable<ObjectTableDependency> GetTableDependencies();
    }
}
