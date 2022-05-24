using System.Collections.Generic;

namespace DKX.Compilation.Resolving
{
    interface IResolver
    {
        IEnumerable<IEntity> ResolveIdentifier(string name);
    }
}
