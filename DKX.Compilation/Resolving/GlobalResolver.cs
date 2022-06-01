using DKX.Compilation.DataTypes;
using DKX.Compilation.Project;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Resolving
{
    class GlobalResolver : IResolver
    {
        private IProject _project;
        private List<string> _usingNamespaces;

        public GlobalResolver(IProject project, IEnumerable<string> usingNamespaces = null)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _usingNamespaces = (usingNamespaces ?? DkxConst.EmptyStringArray).ToList();
        }

        public void AddUsingNamespace(string namespaceName)
        {
            if (_usingNamespaces.Contains(namespaceName)) throw new ArgumentException("Namespace already exists.");
            _usingNamespaces.Add(namespaceName);
        }

        public IClass ResolveClass(string className)
        {
            foreach (var namespaceName in _usingNamespaces)
            {
                var ns = _project.GetNamespaceOrNull(namespaceName);
                var cls = ns?.GetClass(className);
                if (cls != null) return cls;
            }

            return null;
        }

        public IClass GetClassByFullNameOrNull(string fullClassName) => _project.GetClassByFullNameOrNull(fullClassName);

        public IEnumerable<IMethod> GetMethods(DataType dataType, string name)
        {
            if (dataType.BaseType == BaseType.Class)
            {
                var cls = _project.GetClassByFullNameOrNull(dataType.Options[0]);
                return cls?.GetMethods(name) ?? IMethodHelper.EmptyArray;
            }

            return IMethodHelper.EmptyArray;
        }

        public IEnumerable<IField> GetFields(DataType dataType, string name)
        {
            if (dataType.BaseType == BaseType.Class)
            {
                var cls = _project.GetClassByFullNameOrNull(dataType.Options[0]);
                return cls?.GetFields(name) ?? IFieldHelper.EmptyArray;
            }

            return IFieldHelper.EmptyArray;
        }
    }
}
