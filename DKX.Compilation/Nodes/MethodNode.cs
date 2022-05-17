using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Files;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Nodes
{
    class MethodNode : Node, INamedNode, IBodyNode
    {
        private string _name;
        private DataType _returnDataType;
        private Variable[] _args;
        private Privacy _privacy;
        private FileContext _fileContext;
        private CodeSpan _bodySpan;

        public MethodNode(Node parent, string name, DataType returnDataType, IEnumerable<Variable> args, Privacy privacy, FileContext fileContext, CodeSpan bodySpan)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _returnDataType = returnDataType;
            _args = args.ToArray();
            _privacy = privacy;
            _fileContext = fileContext;
            _bodySpan = bodySpan;
        }

        public string Name => _name;

        public ObjectMethod ToObjectFile()
        {
            var context = new OpCodeGeneratorContext(_returnDataType);

            return new ObjectMethod
            {
                Name = _name,
                Privacy = _privacy,
                FileContext = _fileContext,
                ReturnDataType = _returnDataType.ToCode(),
                Arguments = (_args ?? Variable.EmptyArray).Length != 0 ? _args.Select(a => a.ToObjectMethodArgument()).ToArray() : null,
                Body = GenerateObjectBody(context)
            };
        }

        public CodeSpan BodySpan { get => _bodySpan; set => _bodySpan = value; }
    }
}
