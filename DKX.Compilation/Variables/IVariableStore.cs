using System.Collections.Generic;

namespace DKX.Compilation.Variables
{
    public interface IVariableStore
    {
        void AddVariable(Variable variable);

        bool HasVariable(string name, bool includeParents);

        bool TryGetVariable(string name, bool includeParents, out Variable variableOut);

        IEnumerable<Variable> GetVariables(bool includeParents);
    }
}
