using DK;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Files;
using System;

namespace DKX.Compilation.Variables
{
    public class Variable
    {
        private string _name;
        private DataType _dataType;
        private ArgumentPassType? _passType;
        private Chain _initializer;

        public static readonly Variable[] EmptyArray = new Variable[0];

        internal Variable(string name, DataType dataType, ArgumentPassType? passType, Chain initializer)
        {
            _name = name ?? throw new ArgumentNullException();
            if (!_name.IsWord()) throw new ArgumentException("Variable name must be a single word identifier.");

            _dataType = dataType;
            _passType = passType;
            _initializer = initializer;
        }

        public ArgumentPassType? ArgumentType => _passType;
        public DataType DataType => _dataType;
        internal Chain Initializer { get => _initializer; set => _initializer = value ?? throw new ArgumentNullException(); }
        public bool IsArgument => _passType != null;
        public string Name => _name;

        public ObjectMethodArgument ToObjectMethodArgument() => new ObjectMethodArgument
        {
            Name = _name,
            DataType = _dataType.ToCode(),
            PassType = _passType ?? ArgumentPassType.ByReference
        };

        public ObjectMemberVariable ToObjectMemberVariable() => new ObjectMemberVariable
        {
            Name = _name,
            DataType = _dataType.ToCode()
        };
    }
}
