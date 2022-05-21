using DK;
using System.Collections.Generic;

namespace DKX.Compilation
{
    public static class DkxConst
    {
        public const string WorkDirectoryName = ".dkx";
        public const string DkxExtension = ".dkx";
        public const string DkxObjectExtension = ".dkxx";
        public const string WbdkExportsExtension = ".wbdkx";

        public static string[] EmptyStringArray = new string[0];
        public static char[] EmptyCharArray = new char[0];

        /// <summary>
        /// A prefix prepended to a file name to indicate that this is a DKX compiled file.
        /// </summary>
        public const string WbdkFilePrefix = "__";

        /// <summary>
        /// Maximum length of a WBDK file name, without the extension.
        /// For example, "__StringHelper.nc" would get truncated to "__StringHe.nc"
        /// </summary>
        public const int WbdkFileTitleMaxLength = 12;

        public static class Keywords
        {
            public static readonly HashSet<string> AllKeywords = StringHelper.ParseWordList(
                "and bool char class const date enum false if indrel int like namespace numeric or private program protected public " +
                "return short static switch string table time trigger true uint unsigned unsupported ushort var variant void while");

            public static readonly HashSet<string> DataTypeKeyword = StringHelper.ParseWordList(
                "bool char date enum indrel int like numeric short string table time uint unsigned unsupported ushort variant void");

            public const string Class = "class";
            public const string Namespace = "namespace";
        }

        public static class Properties
        {
            public const string SetterArgumentName = "value";
        }

        public static class Numeric
        {
            public const int MaxInt1Digits = 2;
            public const int MaxInt2Digits = 4;
            public const int MaxInt4Digits = 9;
            public const int MaxInt6Digits = 14;
            public const int MaxInt8Digits = 18;
            public const int MaxInt9Digits = 38;

            public const int MaxWidth = 38;
            public const int MinWidth = 1;
            public const int MaxScale = 18;
            public const int MinScale = 1;
        }
    }
}
