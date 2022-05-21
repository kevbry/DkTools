using DKX.Compilation.ReportItems;
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

        [Description("Char literals must contain exactly 1 character.")]
        CharLiteralIsNotSingleCharacter = 1003,
        #endregion

        #region Classes (1100-1199)
        [Description("Expected class name.")]
        ExpectedClassName = 1100,

        [Description("Class name must match the file name (not case sensitive).")]
        ClassNameDoesNotMatchFileName = 1101,

        [Description("Duplicate privacy modifier.")]
        DuplicatePrivacyModifier = 1102,

        [Description("Duplicate context modifier.")]
        DuplicateFileContextModifier = 1103,

        [Description("Invalid server context for this item.")]
        InvalidFileContext = 1104,

        [Description("Duplicate const modifier.")]
        DuplicateConstModifier = 1105,

        [Description("'const' is not valid on this item.")]
        InvalidConst = 1106,

        [Description("Duplicate static modifier.")]
        DuplicateStaticModifier = 1107,

        [Description("'static' is not valid on this item.")]
        InvalidStatic = 1108,

        [Description("Classes must be declared 'static'.")]
        ClassMustBeStatic = 1109,
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

        [Description("Property accessor is more accessible than the property.")]
        PropertyAccessorMoreAccessibleThanProperty = 1302,
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
        [Description("Duplicate '{0}'.")]
        DuplicateVariable = 1401,

        [Description("Expected variable name.")]
        ExpectedVariableName = 1402,

        [Description("Expected argument data type.")]
        ExpectedArgumentDataType = 1403,

        [Description("Expected argument name.")]
        ExpectedArgumentName = 1404,

        [Description("Member variables must be private.")]
        MemberVariableMustBePrivate = 1405,

        [Description("Variable must be initialized with a value.")]
        VariableInitializationRequired = 1406,

        [Description("Invalid data type for a variable.")]
        InvalidVariableDataType = 1407,
        #endregion

        #region Expressions (1500-1599)
        [Description("Expected expression.")]
        ExpectedExpression = 1500,

        /// <summary>
        /// {0} = operator text
        /// </summary>
        [Description("Operator '{0}' expects a value on the right.")]
        OperatorExpectsValueOnRight = 1501,

        [Description("Constants must have an initial value.")]
        ConstantsMustHaveInitializer = 1502,

        [Description("Expected statement.")]
        ExpectedStatement = 1503,

        [Description("Operator '{0}' expected a writeable value on the left.")]
        OperatorExpectsWriteableValueOnLeft = 1504,

        /// <summary>
        /// {0} = identifier name
        /// </summary>
        [Description("Unknown identifier '{0}'.")]
        UnknownIdentifier = 1505,

        /// <summary>
        /// {0} = operator text
        /// </summary>
        [Description("Operator '{0}' cannot be used with this data type.")]
        OperatorCannotBeUsedWithThisDataType = 1506,

        [Description("Condition expression must yield a boolean result.")]
        ConditionMustBeBool = 1507,
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
