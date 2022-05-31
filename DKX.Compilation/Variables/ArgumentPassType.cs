using DKX.Compilation.Exceptions;
using System.IO;

namespace DKX.Compilation.Variables
{
    public enum ArgumentPassType
    {
        ByValue,
        ByReference,
        Out
    }

    public static class ArgumentPassTypeHelper
    {
        public static void Serialize(this ArgumentPassType pt, BinaryWriter bin)
        {
            switch (pt)
            {
                case ArgumentPassType.ByValue:
                    bin.Write((byte)0);
                    break;
                case ArgumentPassType.ByReference:
                    bin.Write((byte)1);
                    break;
                case ArgumentPassType.Out:
                    bin.Write((byte)2);
                    break;
                default:
                    throw new InvalidArgumentPassTypeException();
            }
        }

        public static ArgumentPassType Deserialize(BinaryReader bin)
        {
            switch (bin.ReadByte())
            {
                case 0: return ArgumentPassType.ByValue;
                case 1: return ArgumentPassType.ByReference;
                case 2: return ArgumentPassType.Out;
                default: throw new InvalidArgumentPassTypeException();
            }
        }
    }

    class InvalidArgumentPassTypeException : CompilerException { }
}
