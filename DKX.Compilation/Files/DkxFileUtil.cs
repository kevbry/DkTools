using DK.AppEnvironment;
using DK.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Files
{
    static class DkxFileUtil
    {
        public static FileContext? GetFileContext(string pathName)
        {
            switch (PathUtil.GetExtension(pathName).ToLower())
            {
                case ".ccx":
                    return FileContext.ClientClass;
                case ".ctx":
                    return FileContext.ClientTrigger;
                case ".gpx":
                    return FileContext.GatewayProgram;
                case ".ncx":
                    return FileContext.NeutralClass;
                case ".scx":
                    return FileContext.ServerClass;
                case ".spx":
                    return FileContext.ServerProgram;
                case ".stx":
                    return FileContext.ServerTrigger;
                default:
                    return null;
            }
        }

        public static bool FileIsDkx(string pathName) => GetFileContext(pathName) != null;

        public static string GetWbdkPathName(string dkxPathName)
        {
            switch (PathUtil.GetExtension(dkxPathName).ToLower())
            {
                case ".ccx":
                case ".ctx":
                case ".gpx":
                case ".ncx":
                case ".scx":
                case ".spx":
                case ".stx":
                    return dkxPathName.Substring(0, dkxPathName.Length - 1);    // Just strip off the 'x'
                default:
                    return null;
            }
        }
    }
}
