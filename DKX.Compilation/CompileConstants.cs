using DK;
using System.Collections.Generic;

namespace DKX.Compilation
{
    public static class CompileConstants
    {
        public const string WorkDirectoryName = ".dkx";
        public const string WbdkExportsExtension = ".wbdkx";
        public const string DkxExportsExtension = ".dkxx";

        public static readonly HashSet<string> AllKeywords = StringHelper.ParseWordList("and bool char date enum if  indrel int like numeric or short switch string table time uint unsigned unsupported ushort variant void while");
        public static readonly HashSet<string> DataTypeKeyword = StringHelper.ParseWordList("bool char date enum indrel int like numeric short string table time uint unsigned unsupported ushort variant void");
    }
}
