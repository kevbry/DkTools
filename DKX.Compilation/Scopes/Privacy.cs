using DKX.Compilation.Exceptions;

namespace DKX.Compilation.Scopes
{
    public enum Privacy
    {
        Public,
        Protected,
        Private
    }

    public static class PrivacyHelper
    {
        public static string ToKeyword(this Privacy priv)
        {
            switch (priv)
            {
                case Privacy.Public:
                    return DkxConst.Keywords.Public;
                case Privacy.Protected:
                    return DkxConst.Keywords.Protected;
                case Privacy.Private:
                    return DkxConst.Keywords.Private;
                default:
                    throw new InvalidPrivacyException();
            }
        }
    }

    class InvalidPrivacyException : CompilerException { }
}
