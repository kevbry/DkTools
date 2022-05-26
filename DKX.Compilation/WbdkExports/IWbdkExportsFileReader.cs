using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.WbdkExports
{
    public interface IWbdkExportsFileReader
    {
        Task<IEnumerable<string>> GetIncludeDependenciesAsync();

        Task<IEnumerable<WbdkExportTableDependency>> GetTableDependenciesAsync();
    }
}
