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

        [Description("Invalid char literal.")]
        InvalidCharLiteral = 1004,

        [Description("Invalid string literal.")]
        InvalidStringLiteral = 1005,

        [Description("Invalid type.")]
        InvalidDataType = 1006,
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

        [Description("Expected namespace name.")]
        ExpectedNamespaceName = 1110,

        /// <summary>
        /// {0} = max number of characters
        /// </summary>
        //[Description("Namespace name cannot exceed {0} characters.")]
        //NamespaceNameTooLong = 1111,

        /// <summary>
        /// {0} = class name
        /// </summary>
        [Description("Duplicate class '{0}'.")]
        DuplicateClass = 1112,

        [Description("Type cannot be instantiated.")]
        DataTypeCannotBeInstantiated = 1113,

        /// <summary>
        /// {0} = field name
        /// </summary>
        [Description("Field '{0}' not found.")]
        FieldNotFound = 1114,

        /// <summary>
        /// {0} = field name
        /// </summary>
        [Description("Field '{0}' is ambiguous.")]
        AmbiguousField = 1115,

        /// <summary>
        /// {0} = class name
        /// </summary>
        [Description("Class '{0}' not found.")]
        ClassNotFound = 1116,

        /// <summary>
        /// {0} = constant field name
        /// </summary>
        [Description("A circular dependency was found in constant '{0}'.")]
        CircularConstantDependency = 1117,

        [Description("Namespace '{0}' is not valid here.")]
        NamespaceNotValidHere = 1118,
        #endregion

        #region Methods (1200-1299)
        [Description("Expected method name.")]
        ExpectedMethodName = 1200,

        [Description("No method was found with matching number of arguments.")]
        NoMethodWithSameNumberOfArguments = 1201,

        [Description("No method was found with compatible arguments.")]
        NoMethodWithCompatibleArguments = 1202,

        [Description("More than one method with compatible arguments was found.")]
        MethodAmbiguous = 1203,
        #endregion

        #region Properties (1300-1399)
        [Description("Property has no getter or setter.")]
        PropertyHasNoGetterOrSetter = 1300,

        [Description("Property has no getter.")]
        PropertyHasNoGetter = 1301,

        [Description("Property accessor is more accessible than the property.")]
        PropertyAccessorMoreAccessibleThanProperty = 1302,

        [Description("Duplicate property 'getter'.")]
        DuplicatePropertyGetter = 1303,

        [Description("Duplicate property 'setter'.")]
        DuplicatePropertySetter = 1304,

        /// <summary>
        /// {0} = property name
        /// </summary>
        [Description("Property '{0}' is read-only.")]
        PropertyIsReadOnly = 1305,
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

        [Description("Expected data type.")]
        ExpectedDataType = 1403,

        [Description("Expected argument name.")]
        ExpectedArgumentName = 1404,

        [Description("Member variables must be private.")]
        MemberVariableMustBePrivate = 1405,

        [Description("Variable must be initialized with a value.")]
        VariableInitializationRequired = 1406,

        [Description("Invalid data type for a variable.")]
        InvalidVariableDataType = 1407,

        [Description("Method contains empty arguments.")]
        MethodContainsEmptyArguments = 1408,

        [Description("Duplicate argument name.")]
        DuplicateArgumentName = 1409,

        [Description("Constructor contains empty arguments.")]
        ConstructorContainsEmptyArguments = 1410,

        /// <summary>
        /// {0} = variable name
        /// </summary>
        [Description("Use of variable '{0}' requires a non-static object reference.")]
        VariableRequiresThisPointer = 1411,

        [Description("'this' cannot be modified.")]
        ThisCannotBeModified = 1412,

        [Description("Static class references cannot be modified.")]
        StaticReferenceCannotBeModified = 1413,

        [Description("Invalid number of arguments.")]
        InvalidNumberOfArguments = 1414,
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

        /// <summary>
        /// {0} = operator text
        /// </summary>
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

        [Description("Expression requires a boolean result.")]
        ExpressionMustBeBool = 1507,

        [Description("Expected condition expression.")]
        ExpectedCondition = 1508,

        [Description("Statement is not terminated with ';'.")]
        StatementNotTerminated = 1509,

        [Description("Expression cannot be written to.")]
        ExpressionCannotBeWrittenTo = 1510,

        /// <summary>
        /// {0} = source data type
        /// {1} = destination data type
        /// </summary>
        [Description("Cannot convert '{0}' to '{1}'.")]
        DataTypeNotCompatible = 1511,

        [Description("Possible loss of data converting '{0}' to '{1}'.")]
        [Severity(ErrorSeverity.Warning)]
        DataTypeLossOfDataWarning = 1512,

        /// <summary>
        /// {0} = destination data type
        /// </summary>
        [Description("Constant value is out of bounds for '{0}'.")]
        ConstantDoesNotFit = 1513,

        [Description("Expected member name.")]
        ExpectedMemberName = 1514,

        [Description("Expression must be a constant value.")]
        ExpressionNotConstant = 1515,

        [Description("Division by zero.")]
        DivideByZero = 1516,

        /// <summary>
        /// {0} = data type
        /// </summary>
        [Description("Constant expression yield a value out of range for type '{0}'.")]
        ConstantValueOutOfRange = 1517,

        [Description("Constant could not be resolved.")]
        ConstantNotResolved = 1518,
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
