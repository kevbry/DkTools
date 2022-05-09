using System.Collections.Generic;

namespace DKX.Compilation.WbdkExports
{
    public interface IWbdkExportsFileReader
    {
        IEnumerable<string> GetIncludeDependencies();

        IEnumerable<WbdkExportTableDependency> GetTableDependencies();
    }
}
