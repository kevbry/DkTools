using DKX.Compilation.DataTypes;

namespace DKX.Compilation.Scopes
{
    interface IObjectReferenceScope
    {
        bool ScopeStatic { get; }

        DataType ScopeDataType { get; }
    }
}
