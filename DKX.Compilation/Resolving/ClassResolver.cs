using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Resolving
{
    class ClassResolver : IResolver
    {
        private ClassScope _class;
        private IResolver _parentResolver;

        public ClassResolver(ClassScope class_, IResolver parentResolver)
        {
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));
            _parentResolver = parentResolver ?? throw new ArgumentNullException(nameof(parentResolver));
        }

        public IClass ResolveClass(string className)
        {
            // TODO: Eventually when nested classes are implemented, they will need to be resolved here.
            return _parentResolver.ResolveClass(className);
        }

        public IClass GetClassByFullNameOrNull(string fullClassName) => _parentResolver.GetClassByFullNameOrNull(fullClassName);

        public IEnumerable<IMethod> GetMethods(DataType dataType, string name) => _parentResolver.GetMethods(dataType, name);

        public IEnumerable<IField> GetFields(DataType dataType, string name) => _parentResolver.GetFields(dataType, name);
    }
}
