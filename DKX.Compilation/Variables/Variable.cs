using DK;
using DKX.Compilation.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Variables
{
    public class Variable
    {
        private string _name;
        private DataType _dataType;
        private ArgumentType? _argType;

        public Variable(string name, DataType dataType, ArgumentType? argType)
        {
            _name = name ?? throw new ArgumentNullException();
            if (!_name.IsWord()) throw new ArgumentException("Variable name must be a single word identifier.");

            _dataType = dataType;
            _argType = argType;
        }

        public ArgumentType? ArgumentType => _argType;
        public DataType DataType => _dataType;
        public bool IsArgument => _argType != null;
        public string Name => _name;
    }
}
