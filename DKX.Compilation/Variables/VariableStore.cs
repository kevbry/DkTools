using DKX.Compilation.Scopes;
using System.Collections.Generic;

namespace DKX.Compilation.Variables
{
    class VariableStore : IVariableStore
    {
        private IVariableScope _parent;
        private Dictionary<string, Variable> _variables;

        public VariableStore(IVariableScope parent)
        {
            _parent = parent;
        }

        public void AddVariable(Variable variable)
        {
            if (_variables == null) _variables = new Dictionary<string, Variable>();
            _variables[variable.Name] = variable;
        }

        public void AddVariables(IEnumerable<Variable> variables)
        {
            foreach (var variable in variables) AddVariable(variable);
        }

        public bool HasVariable(string name, bool includeParents, bool localOnly)
        {
            if (_variables != null)
            {
                if (_variables.TryGetValue(name, out var variable))
                {
                    if (localOnly)
                    {
                        if (variable.Local) return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            if (includeParents)
            {
                return _parent?.VariableStore.HasVariable(name, includeParents, localOnly) ?? false;
            }

            return false;
        }

        public bool TryGetVariable(string name, bool includeParents, bool localOnly, out Variable variableOut)
        {
            if (_variables != null)
            {
                if (_variables.TryGetValue(name, out var variable))
                {
                    if (localOnly)
                    {
                        if (variable.Local)
                        {
                            variableOut = variable;
                            return true;
                        }
                    }
                    else
                    {
                        variableOut = variable;
                        return true;
                    }
                }
            }

            if (_variables != null && _variables.TryGetValue(name, out variableOut)) return true;

            if (includeParents)
            {
                if (_parent != null && _parent.VariableStore.TryGetVariable(name, includeParents, localOnly, out variableOut)) return true;
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
