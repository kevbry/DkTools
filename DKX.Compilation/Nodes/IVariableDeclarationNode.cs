using DKX.Compilation.Variables;
using System.Collections.Generic;

namespace DKX.Compilation.Nodes
{
    /// <summary>
    /// The level where a variable is actually declared.
    /// This is different for WBDK as the variable must be declared at the function level, and not within a lower scope.
    /// </summary>
    interface IVariableDeclarationNode
    {
        void AddVariableDeclaration(VariableDeclaration variableDeclaration);

        IEnumerable<VariableDeclaration> GetVariableDeclarations();
    }
}
