using DK.Code;
using System.Collections.Generic;

namespace DKX.Compilation.Files
{
    public interface IObjectFileReader
    {
        ObjectFileModel GetModel();

        string GetDkxPathName();

        IEnumerable<ObjectFileDependency> GetFileDependencies();

        IEnumerable<ObjectTableDependency> GetTableDependencies();
    }
}
