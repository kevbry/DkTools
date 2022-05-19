using DK;
using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Files;
using System;

namespace DKX.Compilation.Variables
{
    public class Variable
    {
        private string _name;
        private string _wbdkName;
        private DataType _dataType;
        private FileContext _fileContext;
        private ArgumentPassType? _passType;
        private Chain _initializer;

        public static readonly Variable[] EmptyArray = new Variable[0];

        internal Variable(string name, string wbdkName, DataType dataType, FileContext fileContext, ArgumentPassType? passType, Chain initializer)
        {
            _name = name ?? throw new ArgumentNullException();
            if (!_name.IsWord()) throw new ArgumentException("Variable name must be a single word identifier.");

            _wbdkName = wbdkName ?? throw new ArgumentNullException(nameof(wbdkName));
            _dataType = dataType;
            _fileContext = fileContext;
            _passType = passType;
            _initializer = initializer;
        }

        public ArgumentPassType? ArgumentType => _passType;
        public DataType DataType => _dataType;
        internal Chain Initializer { get => _initializer; set => _initializer = value ?? throw new ArgumentNullException(); }
        public FileContext FileContext => _fileContext;
        public bool IsArgument => _passType != null;
        public string Name => _name;
        public string WbdkName => _wbdkName;

        public ObjectMethodArgument ToObjectMethodArgument() => new ObjectMethodArgument
        {
            Name = _name,
            DataType = _dataType.ToCode(),
            PassType = _passType ?? ArgumentPassType.ByReference
        };

        public ObjectMemberVariable ToObjectMemberVariable() => new ObjectMemberVariable
        {
            Name = _name,
            FileContext = _fileContext,
            DataType = _dataType.ToCode()
        };

        public ObjectVariable ToObjectVariable()
        {
            OpCodeGenerator code = null;
            if (_initializer != null)
            {
                code = new OpCodeGenerator();
                _initializer.ToCode(code, 0);
            }

            return new ObjectVariable
            {
                Name = _name,
                DataType = _dataType.ToCode(),
                InitializerCode = code?.ToString()
            };
        }
    }
}
