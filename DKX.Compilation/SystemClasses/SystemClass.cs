using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.SystemClasses.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.SystemClasses
{
    class SystemClass : IClass
    {
        public static readonly SystemClass[] SystemClasses = new SystemClass[]
        {
            new SystemConsoleClass()
        };

        private string _className;
        private List<SystemMethod> _methods = new List<SystemMethod>();

        public SystemClass(string className)
        {
            _className = className ?? throw new ArgumentNullException(nameof(className));
        }

        public string ClassName => _className;

        public string FullClassName => "System." + _className;

        public string NamespaceName => "System";

        public string WbdkClassName => FullClassName;

        public string DkxPathName => string.Empty;

        public Privacy Privacy => Privacy.Public;

        public bool Static => true;

        public uint DataSize => 0;

        public IEnumerable<IMethod> Methods => _methods;

        public IEnumerable<IMethod> GetMethods(string name) => _methods.Where(x => x.Name == name);

        public IEnumerable<IField> Fields => IFieldHelper.EmptyArray;

        public IEnumerable<IField> GetFields(string name) => IFieldHelper.EmptyArray;

        protected void AddMethod(SystemMethod method)
        {
            _methods.Add(method);
        }
    }
}
