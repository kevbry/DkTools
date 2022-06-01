using System.Collections.Generic;

namespace DKX.Compilation.Resolving
{
    public interface INamespace
    {
        string NamespaceName { get; }

        IEnumerable<IClass> Classes { get; }

        IClass GetClass(string name);

        NamespaceAccessType AccessType { get; }
    }

    public enum NamespaceAccessType
    {
        Normal,
        System
    }
}
