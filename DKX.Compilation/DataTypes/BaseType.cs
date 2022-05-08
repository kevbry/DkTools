using System;

namespace DKX.Compilation.DataTypes
{
    public enum BaseType : byte
    {
        /// <summary>
        /// Unsupported WBDK data type (e.g. graphic, scroll, interface, etc)
        /// </summary>
        Unsupported,

        /// <summary>
        /// Not a data type. Used for function return types.
        /// </summary>
        Void,

        /// <summary>
        /// Boolean value. Represented as Boolean_t in WBDK.
        /// </summary>
        Bool,

        /// <summary>
        /// 16-bit integer.
        /// </summary>
        Short,

        /// <summary>
        /// 16-bit unsigned integer.
        /// </summary>
        UShort,

        /// <summary>
        /// 32-bit integer.
        /// </summary>
        Int,

        /// <summary>
        /// 32-bit unsigned integer.
        /// </summary>
        UInt,

        /// <summary>
        /// Fixed scale numeric. Includes the width and scale attributes.
        /// </summary>
        Numeric,

        /// <summary>
        /// Fixed scale unsigned numeric. Includes the width and scale attributes.
        /// </summary>
        UNumeric,

        /// <summary>
        /// Single wide character
        /// </summary>
        Char,

        /// <summary>
        /// Single wide unsigned char.
        /// </summary>
        UChar,

        /// <summary>
        /// String. Width field identifies the maximum length (not including null terminator).
        /// </summary>
        String,

        /// <summary>
        /// Date
        /// </summary>
        Date,

        /// <summary>
        ///  Time
        /// </summary>
        Time,

        /// <summary>
        /// Enum list. Options list contains the valid values.
        /// </summary>
        Enum,

        /// <summary>
        /// System function with table argument (e.g. find())
        /// </summary>
        Table,

        /// <summary>
        /// System function with index/relationship argument (e.g. insrel())
        /// </summary>
        Indrel,

        /// <summary>
        /// COM variant
        /// </summary>
        Variant,

        /// <summary>
        /// like operator on a single identifier.
        /// Options list will contain 1 string with the identifier name.
        /// </summary>
        Like1,

        /// <summary>
        /// Like operator on a word-dot-pair.
        /// Options list will contain 2 strings with the 2 words.
        /// </summary>
        Like2
    }

    class InvalidBaseTypeException : Exception
    {
        public InvalidBaseTypeException(BaseType baseType) : base($"Invalid base type '{baseType}'.") { }
    }
}
