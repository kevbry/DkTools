using DK.AppEnvironment;
using DK.Code;

namespace DKX.Compilation.Files
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

        public static string DkxPathNameToWbdkPathName(string dkxPathName, FileContext fileContext)
        {
            var dir = PathUtil.GetDirectoryName(dkxPathName);

            string fileName;
            switch (fileContext)
            {
                case FileContext.GatewayProgram:
                case FileContext.ServerProgram:
                    fileName = PathUtil.GetFileNameWithoutExtension(dkxPathName) + fileContext.GetExtension();
                    break;
                default:
                    fileName = CompileConstants.WbdkFilePrefix + PathUtil.GetFileNameWithoutExtension(dkxPathName) + fileContext.GetExtension();
                    break;
            }

            return PathUtil.CombinePath(dir, fileName);
        }
    }
}
