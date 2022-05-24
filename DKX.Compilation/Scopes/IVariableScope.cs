using DKX.Compilation.Variables;

namespace DKX.Compilation.Scopes
{
    interface IVariableScope
    {
        IVariableStore VariableStore { get; }
    }
}
