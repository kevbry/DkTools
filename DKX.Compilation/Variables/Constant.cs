using DK;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Files;
using System;

namespace DKX.Compilation.Variables
{
    class Constant
    {
        private string _name;
        private DataType _dataType;
        private Chain _value;

        public Constant(string name, DataType dataType, Chain value)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            if (!_name.IsWord()) throw new ArgumentException("Constant name must be a single word.");

            _dataType = dataType;
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name => _name;
        public DataType DataType => _dataType;
        public Chain Value => _value;

        public ObjectConstant ToObjectConstant() => new ObjectConstant
        {
            Name = _name,
            DataType = _dataType.ToCode(),
            Code = _value.ToCode(_value.Span.Start),
            CodeStartPosition = _value.Span.Start
        };
    }
}
