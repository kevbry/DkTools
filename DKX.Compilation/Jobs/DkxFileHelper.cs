using DK.AppEnvironment;
using DK.Code;

namespace DKX.Compilation.Jobs
{
    static class DkxFileHelper
    {
        public static readonly FileContext[] ApplicableFileContexts = new FileContext[]
        {
            FileContext.ClientClass,
            FileContext.ClientTrigger,
            FileContext.GatewayProgram,
            FileContext.NeutralClass,
            FileContext.ServerClass,
            FileContext.ServerProgram,
            FileContext.ServerTrigger
        };

        public static string DkxPathNameToWbdkPathName(string dkxPathName, string relDir, string targetDir, FileContext fileContext)
        {
            var dir = PathUtil.CombinePath(targetDir, relDir);
            var fileName = PathUtil.GetFileNameWithoutExtension(dkxPathName) + fileContext.GetExtension();
            return PathUtil.CombinePath(dir, fileName);
        }
    }
}
