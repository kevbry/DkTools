using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Resolving
{
    class ClassResolver : IResolver
    {
        private ClassScope _class;
        private IResolver _globalResolver;

        public ClassResolver(ClassScope class_, IResolver globalResolver)
        {
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));
            _globalResolver = globalResolver ?? throw new ArgumentNullException(nameof(globalResolver));
        }

        public async Task<IClass> ResolveClassAsync(string className)
        {
            // TODO: Eventually when nested classes are implemented, they will need to be resolved here.
            return await _globalResolver.ResolveClassAsync(className);
        }

        public async Task<IEnumerable<IMethod>> GetMethods(DataType dataType, string name) => await _globalResolver.GetMethods(dataType, name);

        public async Task<IEnumerable<IField>> GetFields(DataType dataType, string name) => await _globalResolver.GetFields(dataType, name);
    }
}
