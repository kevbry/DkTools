using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Resolving
{
    class NamespaceResolver : IResolver
    {
        private NamespaceScope _namespace;
        private IResolver _globalResolver;

        public NamespaceResolver(NamespaceScope namespaceScope, IResolver globalResolver)
        {
            _namespace = namespaceScope ?? throw new ArgumentNullException(nameof(namespaceScope));
            _globalResolver = globalResolver ?? throw new ArgumentNullException(nameof(globalResolver));
        }

        public async Task<IClass> ResolveClassAsync(string className)
        {
            return _namespace.GetClass(className) ?? await _globalResolver.ResolveClassAsync(className);
        }

        public async Task<IEnumerable<IMethod>> GetMethods(DataType dataType, string name) => await _globalResolver.GetMethods(dataType, name);

        public async Task<IEnumerable<IField>> GetFields(DataType dataType, string name) => await _globalResolver.GetFields(dataType, name);
    }
}
