using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables;

using System.Collections.Generic;

namespace DKX.Compilation.Resolving
{
    public interface IMethod
    {
        IClass Class { get; }

        string Name { get; }

        string WbdkName { get; }

        DataType ReturnDataType { get; }

        IArgument[] Arguments { get; }

        ModifierFlags Flags { get; }

        Privacy Privacy { get; }

        MethodAccessType AccessType { get; }

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

    static class IArgumentHelper
    {
        public static bool IsMatch(this IArgument a, IArgument b) => a.Name == b.Name && a.DataType == b.DataType && a.PassType == b.PassType;

        public static bool IsMatch(this IEnumerable<IArgument> a, IEnumerable<IArgument> b)
        {
            if (a == null) return b == null;
            if (b == null) return false;

            var aIter = a.GetEnumerator();
            var bIter = b.GetEnumerator();
            while (true)
            {
                var aOk = aIter.MoveNext();
                var bOk = bIter.MoveNext();
                if (aOk != bOk) return false;
                if (!aOk) break;

                if (!aIter.Current.IsMatch(bIter.Current)) return false;
            }

            return true;
        }
    }

    public enum MethodAccessType
    {
        Normal,
        System
    }
}
