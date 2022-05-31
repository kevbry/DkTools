using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;

namespace DKX.Compilation.Resolving
{
    /// <summary>
    /// A field can be a property, member variable, or constant
    /// </summary>
    public interface IField
    {
        IClass Class { get; }

        string Name { get; }

        DataType DataType { get; }

        bool ReadOnly { get; }

        Privacy ReadPrivacy { get; }

        Privacy WritePrivacy { get; }

        bool Static { get; }

        FieldAccessMethod AccessMethod { get; }

        uint Offset { get; }

        ConstTerm ConstantExpression { get; }

        ConstValue ConstantValue { get; }

        Span DefinitionSpan { get; }
    }

    public static class IFieldHelper
    {
        public static readonly IField[] EmptyArray = new IField[0];
    }
}
