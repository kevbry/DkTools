using DKX.Compilation.DataTypes;

namespace DKX.Compilation.Nodes
{
    interface IReturnTargetNode
    {
        DataType ReturnDataType { get; }
    }
}
