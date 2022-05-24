using DKX.Compilation.Variables;

namespace DKX.Compilation.Scopes
{
    interface IConstantScope
    {
        IConstantStore ConstantStore { get; }
    }
}
