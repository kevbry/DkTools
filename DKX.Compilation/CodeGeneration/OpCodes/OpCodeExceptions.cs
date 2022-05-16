using DKX.Compilation.Exceptions;

namespace DKX.Compilation.CodeGeneration.OpCodes
{
    class OpCodeException : CompilerException
    {
        public OpCodeException() { }
        public OpCodeException(string message) : base(message) { }
    }

    class OpCodeCannotBeExecutedException : OpCodeException { }

    class OpCodeCannotGenerateKnownErrorsException : OpCodeException { }

    class OpCodeCannotBeConstantException : OpCodeException { }
}
