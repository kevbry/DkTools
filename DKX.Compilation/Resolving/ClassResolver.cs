using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using System;
using System.Collections.Generic;

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

        public IClass ResolveClass(string className)
        {
            // TODO: Eventually when nested classes are implemented, they will need to be resolved here.
            return _globalResolver.ResolveClass(className);
        }

        public IClass GetClassByFullNameOrNull(string fullClassName) => _globalResolver.GetClassByFullNameOrNull(fullClassName);

        public IEnumerable<IMethod> GetMethods(DataType dataType, string name) => _globalResolver.GetMethods(dataType, name);

        public IEnumerable<IField> GetFields(DataType dataType, string name) => _globalResolver.GetFields(dataType, name);
    }
}
