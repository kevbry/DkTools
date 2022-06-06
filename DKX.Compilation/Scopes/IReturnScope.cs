using DKX.Compilation.DataTypes;

namespace DKX.Compilation.Scopes
{
    interface IReturnScope
    {
        DataType ReturnDataType { get; }

        bool IsConstructor { get; }
    }
}
