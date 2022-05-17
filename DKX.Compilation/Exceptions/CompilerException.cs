using DK;
using DK.Code;
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
        private CodeSpan _span;
        private ErrorCode _errorCode;
        private object[] _args;

        public CodeException(CodeSpan span, ErrorCode errorCode, params object[] args)
        {
            _span = span;
            _errorCode = errorCode;
            _args = args;
        }

        public CodeException(int pos, ErrorCode errorCode, params object[] args)
        {
            _span = new CodeSpan(pos, pos);
            _errorCode = errorCode;
            _args = args;
        }

        public CodeSpan Span => _span;
        public ErrorCode ErrorCode => _errorCode;
        public object[] Arguments => _args;

        public ReportItem ToReportItem(string pathName, string source)
        {
            var start = _span.Start;
            if (start > source.Length) start = source.Length;

            StringHelper.CalcLineAndPosFromOffset(source, start, out var startLine, out var startOffset);

            var end = _span.End;
            if (end > source.Length) end = source.Length;

            StringHelper.CalcLineAndPosFromOffset(source, end, out var endLine, out var endOffset);

            return new ReportItem(pathName, startLine, startOffset, endLine, endOffset, _errorCode, _args);
        }
    }

    class InvalidOpCodeSourceException : CompilerException
    {
        public InvalidOpCodeSourceException(string message) : base(message) { }
    }

    class InvalidBaseTypeException : CompilerException
    {
        public InvalidBaseTypeException(BaseType baseType) : base($"Invalid base type '{baseType}'.") { }
    }
}
