using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Resolving
{
    public interface IExportsProvider
    {
        Task<INamespaceExport> GetNamespaceAsync(string name);
    }
}
