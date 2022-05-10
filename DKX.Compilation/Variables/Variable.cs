using DK;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Files;
using System;

namespace DKX.Compilation.Variables
{
    public class Variable
    {
        private string _name;
        private DataType _dataType;
        private ArgumentPassType? _passType;

        public static readonly Variable[] EmptyArray = new Variable[0];

        public Variable(string name, DataType dataType, ArgumentPassType? passType)
        {
            _name = name ?? throw new ArgumentNullException();
            if (!_name.IsWord()) throw new ArgumentException("Variable name must be a single word identifier.");

            _dataType = dataType;
            _passType = passType;
        }

        public ArgumentPassType? ArgumentType => _passType;
        public DataType DataType => _dataType;
        public bool IsArgument => _passType != null;
        public string Name => _name;

        public ObjectMethodArgument ToObjectMethodArgument() => new ObjectMethodArgument
        {
            Name = _name,
            DataType = _dataType.ToCode(),
            PassType = _passType ?? ArgumentPassType.ByReference
        };
    }
}
