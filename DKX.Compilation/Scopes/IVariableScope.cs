using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Objects;
using DKX.Compilation.Variables;
using System.Linq;

namespace DKX.Compilation.Scopes
{
    interface IVariableScope
    {
        IVariableStore VariableStore { get; }
    }

    static class IVariableScopeHelper
    {
        public static CodeFragment? GenerateScopeEndingWbdkCode(this IVariableScope scope, CodeWriter cw)
        {
            foreach (var variable in scope.VariableStore.GetVariables(includeParents: false).Where(x => x.ArgumentType == null))
            {
                if (variable.DataType.BaseType == DataTypes.BaseType.Class)
                {
                    cw.Write(ObjectAccess.GenerateLeaveScope(new CodeFragment(variable.WbdkName, variable.DataType, Expressions.OpPrec.None, Span.Empty, readOnly: true)));
                    cw.Write(DkxConst.StatementEndToken);
                    cw.WriteLine();
                }
            }

            return null;
        }
    }
}
