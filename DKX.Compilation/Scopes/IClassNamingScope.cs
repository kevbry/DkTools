using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    /// <summary>
    /// A scope which contributes to a class' full name.
    /// </summary>
    interface IClassNamingScope
    {
        string FullClassName { get; }

        IEnumerable<string> FullClassNameParts { get; }
    }
}
