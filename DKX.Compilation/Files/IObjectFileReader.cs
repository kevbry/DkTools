using System.Collections.Generic;

namespace DKX.Compilation.Files
{
    public interface IObjectFileReader
    {
        IEnumerable<ObjectFileDependency> GetFileDependencies();

        IEnumerable<ObjectTableDependency> GetTableDependencies();
    }
}
