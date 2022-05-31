using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Resolving
{
    class MethodResolver : IResolver
    {
        private MethodScope _method;
        private IResolver _classResolver;

        public MethodResolver(MethodScope method, IResolver classResolver)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
            _classResolver = classResolver ?? throw new ArgumentNullException(nameof(classResolver));
        }

        public IClass ResolveClass(string className) => _classResolver.ResolveClass(className);

        public IClass GetClassByFullNameOrNull(string fullClassName) => _classResolver.GetClassByFullNameOrNull(fullClassName);

        public IEnumerable<IMethod> GetMethods(DataType dataType, string name) => _classResolver.GetMethods(dataType, name);

        public IEnumerable<IField> GetFields(DataType dataType, string name) => _classResolver.GetFields(dataType, name);
    }
}
