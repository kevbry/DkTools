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

        #region General Code Errors (1000-1099)
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

        #region Classes (1100-1199)
        [Description("Expected class name.")]
        ExpectedClassName = 1100,

        [Description("Class name must match the file name (not case sensitive).")]
        ClassNameDoesNotMatchFileName = 1101,
        #endregion

        #region Methods (1200-1299)
        [Description("Expected method name.")]
        ExpectedMethodName = 1200,
        #endregion

        #region Properties (1300-1399)
        [Description("Property has no getter or setter.")]
        PropertyHasNoGetterOrSetter = 1300,

        [Description("Property has no getter.")]
        PropertyHasNoGetter = 1301,
        #endregion

        #region Variables/Arguments (1400-1499)
        /// <summary>
        /// {0} = variable name
        /// </summary>
        [Description("Invalid variable name '{0}'.")]
        InvalidVariableName = 1400,

        /// <summary>
        /// {0} = variable name
        /// </summary>
        [Description("Duplicate variable '{0}'.")]
        DuplicateVariable = 1401,

        [Description("Expected variable name.")]
        ExpectedVariableName = 1402,

        [Description("Expected argument data type.")]
        ExpectedArgumentDataType = 1403,

        [Description("Expected argument name.")]
        ExpectedArgumentName = 1404,

        [Description("Member variables must be private.")]
        MemberVariableMustBePrivate = 1405,
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
