using DKX.Compilation.Scopes;

namespace DKX.Compilation.Resolving
{
    public interface IConstructor
    {
        Privacy Privacy { get; }

        IArgument[] Arguments { get; }
    }
}
