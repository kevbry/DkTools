using DKX.Compilation.Variables;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    /// <summary>
    /// The scope where the variable is actually defined in WBDK.
    /// This is at the top of the function, whereas in DKX variables can be defined elsewhere.
    /// </summary>
    interface IVariableWbdkScope
    {
        void AddWbdkVariable(Variable variable);

        bool HasWbdkVariable(string wbdkName);

        IEnumerable<Variable> GetWbdkVariables();
    }

    static class IVariableWbdkScopeHelper
    {
        public static string GetNewVariableWbdkName(this IVariableWbdkScope scope, string newVariableName)
        {
            if (!scope.HasWbdkVariable(newVariableName)) return newVariableName;

            var index = 0;
            string name;
            do
            {
                name = string.Concat(DkxConst.Variables.SystemVariablePrefix, newVariableName, ++index);
            }
            while (scope.HasWbdkVariable(name));

            return name;
        }
    }
}
