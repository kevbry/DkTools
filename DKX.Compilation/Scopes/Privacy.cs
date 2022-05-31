using DKX.Compilation.Exceptions;
using System.IO;

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

        public static void Serialize(this Privacy priv, BinaryWriter bin)
        {
            switch (priv)
            {
                case Privacy.Public:
                    bin.Write((byte)0);
                    break;
                case Privacy.Private:
                    bin.Write((byte)1);
                    break;
                case Privacy.Protected:
                    bin.Write((byte)2);
                    break;
                default:
                    throw new InvalidPrivacyException();
            }
        }

        public static Privacy Deserialize(BinaryReader bin)
        {
            switch (bin.ReadByte())
            {
                case 0:
                    return Privacy.Public;
                case 1:
                    return Privacy.Private;
                case 2:
                    return Privacy.Protected;
                default:
                    throw new InvalidPrivacyException();
            }
        }
    }

    class InvalidPrivacyException : CompilerException { }
}
