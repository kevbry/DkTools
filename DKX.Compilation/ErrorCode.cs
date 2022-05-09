using System;
using System.ComponentModel;
using System.Linq;

namespace DKX.Compilation
{
    public enum ErrorCode
    {
        #region Compiler Errors
        /// <summary>
        /// {0} = Job description
        /// {1} = Exception
        /// </summary>
        [Description("Compile Job Failed: {0} - {1}")]
        DKX0001_CompileJobFailed = 1,
        #endregion

        #region General Code Errors
        [Description("Syntax Error")]
        SyntaxError = 1000,

        /// <summary>
        /// {0} = token text
        /// </summary>
        [Description("Expected '{0}'.")]
        ExpectedToken = 1001,

        /// <summary>
        /// {0} = token text
        /// </summary>
        [Description("Unexpected '{0}'.")]
        UnexpectedToken = 1002,
        #endregion

        #region Variables (1100-1199)
        /// <summary>
        /// {0} = variable name
        /// </summary>
        [Description("Invalid variable name '{0}'.")]
        InvalidVariableName = 1100,

        /// <summary>
        /// {0} = variable name
        /// </summary>
        [Description("Duplicate variable '{0}'.")]
        DuplicateVariable = 1101,

        [Description("Expected variable name.")]
        ExpectedVariableName = 1102,

        [Description("Expected argument data type.")]
        ExpectedArgumentDataType = 1103,

        [Description("Expected argument name.")]
        ExpectedArgumentName = 1104,
        #endregion
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
