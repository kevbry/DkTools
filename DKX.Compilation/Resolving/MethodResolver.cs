using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task<IClass> ResolveClassAsync(string className) => await _classResolver.ResolveClassAsync(className);

        public async Task<IEnumerable<IMethod>> GetMethods(DataType dataType, string name) => await _classResolver.GetMethods(dataType, name);

        public async Task<IEnumerable<IField>> GetFields(DataType dataType, string name) => await _classResolver.GetFields(dataType, name);
    }
}
