using System.Collections.Generic;

namespace DKX.Compilation.Resolving
{
    public interface INamespaceExport
    {
        IEnumerable<IClass> Classes { get; }

        IClass GetClass(string name);
    }
}
