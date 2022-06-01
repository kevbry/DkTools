using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables;

namespace DKX.Compilation.Resolving
{
    public interface IMethod
    {
        IClass Class { get; }

        string Name { get; }

        string WbdkName { get; }

        DataType ReturnDataType { get; }

        IArgument[] Arguments { get; }

        Privacy Privacy { get; }

        MethodAccessType AccessType { get; }

        bool Static { get; }

        FileContext FileContext { get; }

        Span DefinitionSpan { get; }
    }

    public class IMethodHelper
    {
        public static readonly IMethod[] EmptyArray = new IMethod[0];
    }

    public interface IArgument
    {
        string Name { get; }

        DataType DataType { get; }

        ArgumentPassType PassType { get; }
    }

    public enum MethodAccessType
    {
        Normal,
        System
    }
}
