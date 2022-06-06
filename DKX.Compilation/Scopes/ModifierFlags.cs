namespace DKX.Compilation.Scopes
{
    public enum ModifierFlags : uint
    {
        /// <summary>
        /// Static member which does not require an object pointer.
        /// </summary>
        Static = 0x01,

        /// <summary>
        /// A method that is used for a specific purpose and cannot be called directly.
        /// </summary>
        NotCallable = 0x02,

        /// <summary>
        /// This is the entry point (main) for a server/gateway program.
        /// </summary>
        ProgramEntryPoint = 0x04,

        /// <summary>
        /// The field is read-only.
        /// </summary>
        ReadOnly = 0x08,

        /// <summary>
        /// This method is a constructor and only to be used when instantiating an object.
        /// It will always be paired with NotCallable.
        /// </summary>
        Constructor = 0x10,
    }

    public static class ModifierFlagsHelper
    {
        public static bool IsStatic(this ModifierFlags flags) => flags.HasFlag(ModifierFlags.Static);

        public static bool IsReadOnly(this ModifierFlags flags) => flags.HasFlag(ModifierFlags.ReadOnly);

        public static bool IsConstructor(this ModifierFlags flags) => flags.HasFlag(ModifierFlags.Constructor);
    }
}
