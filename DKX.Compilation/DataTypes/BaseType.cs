using DKX.Compilation.Exceptions;
using System;
using System.IO;

namespace DKX.Compilation.DataTypes
{
    public enum BaseType : byte
    {
        /// <summary>
        /// Unsupported WBDK data type (e.g. graphic, scroll, interface, etc)
        /// </summary>
        Unsupported = 0,

        /// <summary>
        /// Not a data type. Used for function return types.
        /// </summary>
        Void = 1,

        /// <summary>
        /// Boolean value. Represented as Boolean_t in WBDK.
        /// </summary>
        Bool = 2,

        /// <summary>
        /// 16-bit integer.
        /// </summary>
        Short = 3,

        /// <summary>
        /// 16-bit unsigned integer.
        /// </summary>
        UShort = 4,

        /// <summary>
        /// 32-bit integer.
        /// </summary>
        Int = 5,

        /// <summary>
        /// 32-bit unsigned integer.
        /// </summary>
        UInt = 6,

        /// <summary>
        /// Fixed scale numeric. Includes the width and scale attributes.
        /// </summary>
        Numeric = 7,

        /// <summary>
        /// Fixed scale unsigned numeric. Includes the width and scale attributes.
        /// </summary>
        UNumeric = 8,

        /// <summary>
        /// Single wide character
        /// </summary>
        Char = 9,

        /// <summary>
        /// String. Width field identifies the maximum length (not including null terminator).
        /// </summary>
        String = 10,

        /// <summary>
        /// Date
        /// </summary>
        Date = 11,

        /// <summary>
        ///  Time
        /// </summary>
        Time = 12,

        /// <summary>
        /// Enum list. Options list contains the valid values.
        /// </summary>
        Enum = 13,

        /// <summary>
        /// System function with table argument (e.g. find())
        /// </summary>
        Table = 14,

        /// <summary>
        /// System function with index/relationship argument (e.g. insrel())
        /// </summary>
        Indrel = 15,

        /// <summary>
        /// COM variant
        /// </summary>
        Variant = 16,

        /// <summary>
        /// A reference to a class object.
        /// Options string contains the full class name. (only 1 element)
        /// </summary>
        Class = 17
    }

    /*
    switch (baseType)
    {
        case BaseType.Unsupported:
        case BaseType.Void:
        case BaseType.Bool:
        case BaseType.Short:
        case BaseType.UShort:
        case BaseType.Int:
        case BaseType.UInt:
        case BaseType.Numeric:
        case BaseType.UNumeric:
        case BaseType.Char:
        case BaseType.String:
        case BaseType.Date:
        case BaseType.Time:
        case BaseType.Enum:
        case BaseType.Table:
        case BaseType.Indrel:
        case BaseType.Variant:
        case BaseType.Class:
    }
    */

    static class BaseTypeHelper
    {
        public static bool IsSigned(this BaseType baseType)
        {
            switch (baseType)
            {
                case BaseType.Char:
                case BaseType.Short:
                case BaseType.Int:
                case BaseType.Numeric:
                    return true;
                default:
                    return false;
            }
        }

        public static int MaxWidth(this BaseType baseType)
        {
            // Signed: 1-2 = int1, 3-4 = int2, 5-9 = int4, 10-14 = int6, 15-18 = int8, 19+ = int9
            switch (baseType)
            {
                case BaseType.Bool:
                case BaseType.Char:
                    return 1;
                case BaseType.Short:
                case BaseType.UShort:
                case BaseType.Date:
                case BaseType.Time:
                case BaseType.Enum:
                    return 4;
                case BaseType.Int:
                case BaseType.UInt:
                    return 9;
                case BaseType.Numeric:
                case BaseType.UNumeric:
                    return 38;
                case BaseType.String:
                    return 255;
                default:
                    return 0;
            }
        }

        public static void Serialize(this BaseType baseType, BinaryWriter bin)
        {
            bin.Write((byte)baseType);
        }

        public static BaseType Deserialize(BinaryReader bin)
        {
            var value = bin.ReadByte();
            if (!Enum.IsDefined(typeof(BaseType), value)) throw new InvalidBaseTypeException();
            return (BaseType)value;
        }
    }
}
