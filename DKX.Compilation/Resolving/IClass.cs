using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Resolving
{
    public interface IClass
    {
        string Name { get; }

        string FullClassName { get; }

        IEnumerable<string> FullClassNameParts { get; }

        Privacy Privacy { get; }

        bool Static { get; }

        uint DataSize { get; }

        Task<IEnumerable<IMethod>> GetMethods(string name);

        Task<IEnumerable<IField>> GetFields(string name);
    }

    public interface IConstructorExport
    {
        Privacy Privacy { get; }

        IArgument[] Arguments { get; }
    }

    public interface IMethod
    {
        string Name { get; }

        DataType ReturnDataType { get; }

        IArgument[] Arguments { get; }

        Privacy Privacy { get; }

        bool Static { get; }

        FileContext FileContext { get; }

        Task<CodeFragment> ToWbdkCode_MethodCallAsync(CodeFragment parentFragment, IEnumerable<CodeFragment> arguments, CodeSpan span);
    }

    public class IMethodHelper
    {
        public static readonly IMethod[] EmptyArray = new IMethod[0];
    }

    public interface IArgument
    {
        string Name { get; }

        DataType DataType { get; }

        ArgumentPassType PassType { get; }
    }

    /// <summary>
    /// A field can be a property, member variable, or constant
    /// </summary>
    public interface IField
    {
        string Name { get; }

        DataType DataType { get; }

        bool ReadOnly { get; }

        Privacy ReadPrivacy { get; }

        Privacy WritePrivacy { get; }

        bool Static { get; }

        bool IsConstant { get; }

        Task<CodeFragment> ToWbdkCode_ReadAsync(CodeFragment parentFragment, CodeSpan fieldSpan, ISourceCodeReporter report);

        Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment parentFragment, CodeSpan fieldSpan, CodeFragment valueFragment, ISourceCodeReporter report);
    }

    public class IFieldHelper
    {
        public static readonly IField[] EmptyArray = new IField[0];
    }
}
