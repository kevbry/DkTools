using DK;
using System.Collections.Generic;

namespace DKX.Compilation
{
    public static class CompileConstants
    {
        public const string WorkDirectoryName = ".dkx";
        public const string DkxExtension = ".dkx";
        public const string DkxObjectExtension = ".dkxx";
        public const string WbdkExportsExtension = ".wbdkx";

        /// <summary>
        /// A prefix prepended to a file name to indicate that this is a DKX compiled file.
        /// </summary>
        public const string WbdkFilePrefix = "__";

        /// <summary>
        /// Maximum length of a WBDK file name, without the extension.
        /// For example, "__StringHelper.nc" would get truncated to "__StringHe.nc"
        /// </summary>
        public const int WbdkFileTitleMaxLength = 12;

        public static readonly HashSet<string> AllKeywords = StringHelper.ParseWordList(
            "and bool char class const date enum if indrel int numeric or private program protected public" +
            "short switch string table time trigger uint unsigned unsupported ushort variant void while");

        public static readonly HashSet<string> DataTypeKeyword = StringHelper.ParseWordList(
            "bool char date enum indrel int numeric short string table time uint unsigned unsupported ushort variant void");
    }
}
