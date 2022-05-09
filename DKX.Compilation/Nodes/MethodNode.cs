using DKX.Compilation.DataTypes;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Nodes
{
    class MethodNode : Node
    {
        private string _name;
        private DataType _returnDataType;
        private Variable[] _args;

        public MethodNode(Node parent, string name, DataType returnDataType, IEnumerable<Variable> args)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _returnDataType = returnDataType;
            _args = args.ToArray();
        }
    }
}
