using DK.Code;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.ObjectFiles
{
    public interface IObjectFileReader
    {
        Task<IEnumerable<string>> GetFileDependenciesAsync();

        Task<IDictionary<string, string>> GetTableDependenciesAsync();

        Task<IEnumerable<FileContext>> GetFileContextsAsync();
    }
}
