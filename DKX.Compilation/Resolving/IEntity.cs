using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Resolving
{
    public interface IEntity
    {
        DataType DataType { get; }

        Chain ToWbdkCode(ISourceCodeReporter report);

        Chain ToWbdkConstant(ISourceCodeReporter report);
    }
}
