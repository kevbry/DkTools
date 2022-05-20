using DKX.Compilation.Variables;
using System.Collections.Generic;

namespace DKX.Compilation.Nodes
{
    /// <summary>
    /// The level where a variable will be considered 'in scope' in DKX.
    /// </summary>
    interface IVariableScopeNode
    {
        IVariableStore VariableStore { get; }
    }

    interface IVariableStore
    {
        void AddVariable(Variable variable);

        bool HasVariable(string name, bool includeParents);

        bool TryGetVariable(string name, bool includeParents, out Variable variableOut);

        IEnumerable<Variable> GetVariables(bool includeParents);
    }
}
