using DKX.Compilation.Exceptions;
using System.IO;

namespace DKX.Compilation.Variables
{
    public enum FieldAccessMethod
    {
        Variable,
        Object,
        Property,
        Constant
    }

    public static class FieldAccessMethodHelper
    {
        public static void Serialize(this FieldAccessMethod fam, BinaryWriter bin)
        {
            switch (fam)
            {
                case FieldAccessMethod.Variable:
                    bin.Write((byte)0);
                    break;
                case FieldAccessMethod.Object:
                    bin.Write((byte)1);
                    break;
                case FieldAccessMethod.Property:
                    bin.Write((byte)2);
                    break;
                case FieldAccessMethod.Constant:
                    bin.Write((byte)3);
                    break;
                default:
                    throw new InvalidFieldAccessMethodException();
            }
        }

        public static FieldAccessMethod Deserialize(BinaryReader bin)
        {
            switch (bin.ReadByte())
            {
                case 0: return FieldAccessMethod.Variable;
                case 1: return FieldAccessMethod.Object;
                case 2: return FieldAccessMethod.Property;
                case 3: return FieldAccessMethod.Constant;
                default: throw new InvalidFieldAccessMethodException();
            }
        }
    }

    class InvalidFieldAccessMethodException : CompilerException { }
}
