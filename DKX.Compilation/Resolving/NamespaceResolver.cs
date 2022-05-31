using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using System;
using System.Collections.Generic;

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

        public IClass ResolveClass(string className) => _namespace.GetClass(className) ?? _globalResolver.ResolveClass(className);

        public IClass GetClassByFullNameOrNull(string fullClassName) => _globalResolver.GetClassByFullNameOrNull(fullClassName);

        public IEnumerable<IMethod> GetMethods(DataType dataType, string name) => _globalResolver.GetMethods(dataType, name);

        public IEnumerable<IField> GetFields(DataType dataType, string name) => _globalResolver.GetFields(dataType, name);
    }
}
