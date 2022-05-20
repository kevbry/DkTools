using DKX.Compilation.Nodes;
using System.Collections.Generic;

namespace DKX.Compilation.Variables
{
    class VariableStore : IVariableStore
    {
        private IVariableScopeNode _parent;
        private Dictionary<string, Variable> _variables;

        public VariableStore(IVariableScopeNode parent)
        {
            _parent = parent;
        }

        public void AddVariable(Variable variable)
        {
            if (_variables == null) _variables = new Dictionary<string, Variable>();
            _variables[variable.Name] = variable;
        }

        public bool HasVariable(string name, bool includeParents)
        {
            if (_variables != null && _variables.ContainsKey(name)) return true;

            if (includeParents)
            {
                return _parent?.VariableStore.HasVariable(name, includeParents) ?? false;
            }

            return false;
        }

        public bool TryGetVariable(string name, bool includeParents, out Variable variableOut)
        {
            if (_variables != null && _variables.TryGetValue(name, out variableOut)) return true;

            if (includeParents)
            {
                if (_parent != null && _parent.VariableStore.TryGetVariable(name, includeParents, out variableOut)) return true;
            }

            variableOut = default;
            return false;
        }

        public IEnumerable<Variable> GetVariables(bool includeParents)
        {
            if (_variables != null)
            {
                foreach (var variable in _variables.Values)
                {
                    yield return variable;
                }
            }

            if (includeParents)
            {
                if (_parent != null)
                {
                    foreach (var variable in _parent.VariableStore.GetVariables(includeParents))
                    {
                        yield return variable;
                    }
                }
            }
        }
    }
}
