using DK;
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
        private ClassScope _class;
        private string _name;
        private DataType _dataType;
        private Privacy _privacy;
        private ConstTerm _constTerm;
        private Span _span;

        public Constant(ClassScope class_, string name, DataType dataType, ConstTerm constTerm, Privacy privacy, Span span)
        {
            _class = class_ ?? throw new ArgumentNullException(nameof(class_));

            _name = name ?? throw new ArgumentNullException(nameof(name));
            if (!_name.IsWord()) throw new ArgumentException("Constant name must be a single word.");

            _dataType = dataType;
            _privacy = privacy;
            _constTerm = constTerm;
            _span = span;
        }

        public FieldAccessMethod AccessMethod => FieldAccessMethod.Constant;
        public ClassScope Class => _class;
        IClass IField.Class => _class;
        public ConstTerm ConstantExpression => _constTerm;
        public ConstValue ConstantValue => null;
        public DataType DataType => _dataType;
        public Span DefinitionSpan => _span;
        public bool IsConstant => true;
        public string Name => _name;
        public uint Offset => default;
        public bool ReadOnly => true;
        public Privacy ReadPrivacy => _privacy;
        public bool Static => true;
        public Privacy WritePrivacy => _privacy;
    }
}
