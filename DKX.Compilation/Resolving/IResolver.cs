using DKX.Compilation.DataTypes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Resolving
{
    public interface IResolver
    {
        Task<IClass> ResolveClassAsync(string className);

        Task<IEnumerable<IMethod>> GetMethods(DataType dataType, string name);

        Task<IEnumerable<IField>> GetFields(DataType dataType, string name);
    }
}
