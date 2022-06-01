using DKX.Compilation.Scopes;
using System.Collections.Generic;

namespace DKX.Compilation.Variables
{
    class ConstantStore : IConstantStore
    {
        private IConstantScope _parent;
        private Dictionary<string, Constant> _constants;

        public ConstantStore(IConstantScope parentStore)
        {
            _parent = parentStore;
        }

        public IEnumerable<Constant> Constants => (IEnumerable<Constant>)_constants?.Values ?? Constant.EmptyArray;

        public void Add(Constant constant)
        {
            if (_constants == null) _constants = new Dictionary<string, Constant>();
            _constants[constant.Name] = constant;
        }

        public bool TryGetConstant(string name, out Constant constantOut)
        {
            if (_constants != null && _constants.TryGetValue(name, out var constant))
            {
                constantOut = constant;
                return true;
            }

            constantOut = null;
            return false;
        }

        public IEnumerable<Constant> GetConstants(string name)
        {
            if (_constants != null && _constants.TryGetValue(name, out var constant)) yield return constant;
        }
    }
}
