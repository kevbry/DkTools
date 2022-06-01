using DK;
using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Variables
{
    class Constant : IField
    {
        public static readonly Constant[] EmptyArray = new Constant[0];

        private IClass _class;
        private string _name;
        private DataType _dataType;
        private Privacy _privacy;
        private ConstTerm _constTerm;
        private ConstValue _constValue;
        private Span _span;

        public Constant(IClass class_, string name, DataType dataType, ConstTerm constTerm, ConstValue constValueOrNull, Privacy privacy, Span span)
        {
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));

            _name = name ?? throw new ArgumentNullException(nameof(name));
            if (!_name.IsWord()) throw new ArgumentException("Constant name must be a single word.");

            _dataType = dataType;
            _privacy = privacy;
            _constTerm = constTerm;
            _constValue = constValueOrNull;
            _span = span;
        }

        public Constant(IField field)
        {
            _class = field.Class;
            _name = field.Name;
            _dataType = field.DataType;
            _privacy = field.ReadPrivacy;
            _constValue = field.ConstantValue;
            _span = field.DefinitionSpan;
        }

        public FieldAccessMethod AccessMethod => FieldAccessMethod.Constant;
        public IClass Class => _class;
        IClass IField.Class => _class;
        public ConstTerm ConstantExpression => _constTerm;
        public ConstValue ConstantValue => _constValue;
        public DataType DataType => _dataType;
        public Span DefinitionSpan => _span;
        public FileContext FileContext => FileContext.NeutralClass;
        public bool IsConstant => true;
        public string Name => _name;
        public uint Offset => default;
        public bool ReadOnly => true;
        public Privacy ReadPrivacy => _privacy;
        public bool Static => true;
        public Privacy WritePrivacy => _privacy;
    }
}
