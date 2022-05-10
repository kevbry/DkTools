using DKX.Compilation.DataTypes;
using DKX.Compilation.Files;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Nodes
{
    class MethodNode : Node, INamedNode
    {
        private string _name;
        private DataType _returnDataType;
        private Variable[] _args;
        private Privacy _privacy;

        public MethodNode(Node parent, string name, DataType returnDataType, IEnumerable<Variable> args, Privacy privacy)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _returnDataType = returnDataType;
            _args = args.ToArray();
            _privacy = privacy;
        }

        public string Name => _name;

        public ObjectMethod ToObjectFile() => new ObjectMethod
        {
            Name = _name,
            Privacy = _privacy,
            ReturnDataType = _returnDataType.ToCode(),
            Arguments = (_args ?? Variable.EmptyArray).Length != 0 ? _args.Select(a => a.ToObjectMethodArgument()).ToArray() : null,
            Body = GenerateObjectBody()
        };
    }
}
