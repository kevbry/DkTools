using DKX.Compilation.Scopes;
using System.Collections.Generic;

namespace DKX.Compilation.Resolving
{
    public interface IClass
    {
        string ClassName { get; }

        string FullClassName { get; }

        string NamespaceName { get; }

        string WbdkClassName { get; }

        string DkxPathName { get; }

        Privacy Privacy { get; }

        ModifierFlags Flags { get; }

        uint DataSize { get; }

        Span NameSpan { get; }

        IEnumerable<IMethod> Methods { get; }

        IEnumerable<IMethod> GetMethods(string name);

        IEnumerable<IField> Fields { get; }

        IEnumerable<IField> GetFields(string name);
    }
}
