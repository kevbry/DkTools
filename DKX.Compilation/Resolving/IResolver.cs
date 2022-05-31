using DKX.Compilation.DataTypes;
using System.Collections.Generic;

namespace DKX.Compilation.Resolving
{
    public interface IResolver
    {
        IClass ResolveClass(string className);

        IClass GetClassByFullNameOrNull(string fullClassName);

        IEnumerable<IMethod> GetMethods(DataType dataType, string name);

        IEnumerable<IField> GetFields(DataType dataType, string name);
    }
}
