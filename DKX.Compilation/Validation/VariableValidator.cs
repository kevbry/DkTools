using DK;

namespace DKX.Compilation.Validation
{
    static class VariableValidator
    {
        public static bool IsValidVariableName(string name)
        {
            if (!name.IsWord()) return false;
            if (DkxConst.Keywords.AllKeywords.Contains(name)) return false;
            if (name.StartsWith(DkxConst.Variables.SystemVariablePrefix)) return false;

            return true;
        }
    }
}
