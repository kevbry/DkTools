using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Exceptions
{
    public abstract class CompilerException : Exception
    {
        public CompilerException() { }

        public CompilerException(string message) : base(message) { }
    }

    class InvalidObjectFileException : CompilerException
    {
        public InvalidObjectFileException(string pathName) : base($"Object file '{pathName}' does not have a correct format.") { }
    }

    class CodeException : CompilerException
    {
        private Span _span;
        private ErrorCode _errorCode;
        private object[] _args;

        public CodeException(Span span, ErrorCode errorCode, params object[] args)
        {
            _span = span;
            _errorCode = errorCode;
            _args = args;
        }

        public Span Span => _span;
        public ErrorCode ErrorCode => _errorCode;
        public object[] Arguments => _args;

        public ReportItem ToReportItem() => new ReportItem(_span, _errorCode, _args);
    }

    class InvalidOpCodeSourceException : CompilerException
    {
        public InvalidOpCodeSourceException(string message) : base(message) { }
    }

    class InvalidBaseTypeException : CompilerException
    {
        public InvalidBaseTypeException() : base("Invalid base type.") { }
        public InvalidBaseTypeException(BaseType baseType) : base($"Invalid base type '{baseType}'.") { }
    }
}
