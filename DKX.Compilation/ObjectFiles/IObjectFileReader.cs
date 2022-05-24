using DK.Code;
using System.Collections.Generic;

namespace DKX.Compilation.ObjectFiles
{
    public interface IObjectFileReader
    {
        IEnumerable<string> GetFileDependencies();

        IDictionary<string, string> GetTableDependencies();

        IEnumerable<FileContext> GetFileContexts();
    }
}
