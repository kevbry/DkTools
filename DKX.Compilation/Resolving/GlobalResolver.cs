using DKX.Compilation.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Resolving
{
    class GlobalResolver : IResolver
    {
        private IExportsProvider _exports;
        private string[] _usingNamespaces;

        public GlobalResolver(IExportsProvider exports, IEnumerable<string> usingNamespaces)
        {
            _exports = exports ?? throw new ArgumentNullException(nameof(exports));
            _usingNamespaces = (usingNamespaces ?? throw new ArgumentNullException(nameof(usingNamespaces))).ToArray();
        }

        public async Task<IClass> ResolveClassAsync(string className)
        {
            foreach (var nsName in _usingNamespaces)
            {
                var ns = await _exports.GetNamespaceAsync(nsName);
                var cls = ns?.GetClass(className);
                if (cls != null) return cls;
            }

            return null;
        }

        public async Task<IEnumerable<IMethod>> GetMethods(DataType dataType, string name)
        {
            if (dataType.BaseType == BaseType.Class)
            {
                var cls = dataType.Class;
                return await cls.GetMethods(name);
            }

            return IMethodHelper.EmptyArray;
        }

        public async Task<IEnumerable<IField>> GetFields(DataType dataType, string name)
        {
            if (dataType.BaseType == BaseType.Class)
            {
                var cls = dataType.Class;
                return await cls.GetFields(name);
            }

            return IFieldHelper.EmptyArray;
        }
    }
}
