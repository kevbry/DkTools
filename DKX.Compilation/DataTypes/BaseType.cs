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
        /// Single wide unsigned char.
        /// </summary>
        UChar = 10,

        /// <summary>
        /// String. Width field identifies the maximum length (not including null terminator).
        /// </summary>
        String = 11,

        /// <summary>
        /// Date
        /// </summary>
        Date = 12,

        /// <summary>
        ///  Time
        /// </summary>
        Time = 13,

        /// <summary>
        /// Enum list. Options list contains the valid values.
        /// </summary>
        Enum = 14,

        /// <summary>
        /// System function with table argument (e.g. find())
        /// </summary>
        Table = 15,

        /// <summary>
        /// System function with index/relationship argument (e.g. insrel())
        /// </summary>
        Indrel = 16,

        /// <summary>
        /// COM variant
        /// </summary>
        Variant = 17
    }
}
