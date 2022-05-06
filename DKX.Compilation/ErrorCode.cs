using System;
using System.ComponentModel;
using System.Linq;

namespace DKX.Compilation
{
    public enum ErrorCode
    {
        [Description("Compile Job Failed: {0} - {1}")]
        DKX0001_CompileJobFailed = 1,

        [Description("Syntax Error")]
        DKX1000_SyntaxError = 1000
    }

    public static class CompileErrorUtil
    {
        public static string GetDescription(this ErrorCode code)
        {
            return typeof(ErrorCode)
                .GetMember(code.ToString())
                .First()
                .GetCustomAttributes(typeof(DescriptionAttribute), inherit: false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault()
                ?.Description
                ?? code.ToString();
        }

        public static ErrorSeverity GetSeverity(this ErrorCode code)
        {
            return typeof(ErrorCode)
                .GetMember(code.ToString())
                .First()
                .GetCustomAttributes(typeof(SeverityAttribute), inherit: false)
                .Cast<SeverityAttribute>()
                .FirstOrDefault()
                ?.Severity
                ?? ErrorSeverity.Error;
        }
    }

    class SeverityAttribute : Attribute
    {
        private ErrorSeverity _severity;

        public SeverityAttribute(ErrorSeverity severity)
        {
            _severity = severity;
        }

        public ErrorSeverity Severity => _severity;
    }
}
