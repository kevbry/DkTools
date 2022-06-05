using System.Collections.Generic;

namespace DKX.Compilation
{
    public static class DkxConst
    {
        public const string WorkDirectoryName = ".dkx";
        public const string DkxExtension = ".dkx";
        public const string DkxObjectExtension = ".dkxx";
        public const string WbdkExportsExtension = ".wbdkx";
        public const string ClassHashPrefix = "c";
        public const string ProjectFileName = "dkx.dat";

        public static string[] EmptyStringArray = new string[0];
        public static char[] EmptyCharArray = new char[0];

        public const char StatementEndToken = ';';
        public const char DelimiterToken = ',';

        public const string This = "this";

        public static class Namespaces
        {
            //public const int MaxNamespaceLength = 12;
        }

        public static class Properties
        {
            public const string SetterArgumentName = "value";
            public const string GetterPrefix = "Get";
            public const string SetterPrefix = "Set";
        }

        public static class Variables
        {
            public const string UnnamedArgumentFormat = "unnamed{0}";
            public const string SystemVariablePrefix = "__";
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
            public const int MinScale = 0;

            public const int MaxLiteralDigits = 28;
        }

        public static class String
        {
            public const int MinLength = 1;
            public const int MaxLength = 255;
        }

        public static class Operators
        {
            public const string Dot = ".";
            public const char DotChar = '.';
        }

        #region Keywords
        public static class Keywords
        {
            public static readonly HashSet<string> AllKeywords = new HashSet<string>(new string[]
            {
                And, Break, Bool, Char, Class, Client, Const, Continue, Date, Do, Else, Enum, False, For, Get, If, Indrel, Int,
                Namespace, Neutral, New, Numeric, Or, Private, Protected, Public, Return, Server, Set, Short, Static, Switch, String,
                Table, This, Time, True, Typedef, UInt, Unsigned, Unsupported, UShort, Using, Var, Variant, Void, While
            });

            public static readonly HashSet<string> DataTypeKeyword = new HashSet<string>(new string[]
            {
                Bool, Char, Date, Enum, Indrel, Int, Numeric, Short, String, Table, Time, UInt, Unsigned, Unsupported, UShort, Variant, Void
            });

            /// <summary>
            /// Keywords that kick off a statement. (e.g. if, for, while, return, etc...)
            /// Does not include keywords that occur in a statement, but not as the first keyword, such as 'else'.
            /// </summary>
            public static readonly HashSet<string> ControlStatementStartKeyword = new HashSet<string>(new string[]
            {
                Break, Continue, Do, If, For, Return, Switch, Var, While
            });

            public const string And = "and";
            public const string Break = "break";
            public const string Bool = "bool";
            public const string Char = "char";
            public const string Class = "class";
            public const string Client = "client";
            public const string Const = "const";
            public const string Continue = "continue";
            public const string Date = "date";
            public const string Do = "do";
            public const string Else = "else";
            public const string Enum = "enum";
            public const string False = "false";
            public const string For = "for";
            public const string Get = "get";
            public const string If = "if";
            public const string Indrel = "indrel";
            public const string Int = "int";
            public const string Namespace = "namespace";
            public const string Neutral = "neutral";
            public const string New = "new";
            public const string Numeric = "numeric";
            public const string Or = "or";
            public const string Out = "out";
            public const string Private = "private";
            public const string Protected = "protected";
            public const string Public = "public";
            public const string Ref = "ref";
            public const string Return = "return";
            public const string Server = "server";
            public const string Set = "set";
            public const string Short = "short";
            public const string Static = "static";
            public const string String = "string";
            public const string Switch = "switch";
            public const string Table = "table";
            public const string Typedef = "typedef";
            public const string This = "this";
            public const string Time = "time";
            public const string True = "true";
            public const string UInt = "uint";
            public const string UShort = "ushort";
            public const string Using = "using";
            public const string Unsigned = "unsigned";
            public const string Unsupported = "unsupported";
            public const string Var = "var";
            public const string Variant = "variant";
            public const string Void = "void";
            public const string While = "while";
        }
        #endregion

        #region DkxLib
        public static class DkxLib
        {
            public const string dkx_new = "dkx_new";
        }
        #endregion

        #region Attributes
        public static class Attributes
        {
            public const string ServerProgram = "ServerProgram";
            public const string GatewayProgram = "GatewayProgram";
        }
        #endregion
    }
}
