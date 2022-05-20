using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Files;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Nodes
{
    class MethodNode : Node, INamedNode, IBodyNode, IVariableScopeNode, IVariableDeclarationNode
    {
        private string _name;
        private DataType _returnDataType;
        private Variable[] _args;
        private Privacy _privacy;
        private FileContext _fileContext;
        private CodeSpan _bodySpan;
        private List<VariableDeclaration> _variableDeclarations;
        private VariableStore _variableStore;

        public MethodNode(Node parent, string name, DataType returnDataType, IEnumerable<Variable> args, Privacy privacy, FileContext fileContext, CodeSpan bodySpan)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _returnDataType = returnDataType;
            _args = args.ToArray();
            _privacy = privacy;
            _fileContext = fileContext;
            _bodySpan = bodySpan;
            _variableStore = new VariableStore(parent?.GetContainerOrNull<IVariableScopeNode>());
        }

        public string Name => _name;

        public ObjectMethod ToObjectFile() => new ObjectMethod
        {
            Name = _name,
            Privacy = _privacy,
            FileContext = _fileContext,
            ReturnDataType = _returnDataType.ToCode(),
            Arguments = (_args ?? Variable.EmptyArray).Length != 0 ? _args.Select(a => a.ToObjectMethodArgument()).ToArray() : null,
            Body = GenerateObjectBody(_bodySpan.Start)
        };

        public CodeSpan BodySpan { get => _bodySpan; set => _bodySpan = value; }

        public void AddVariableDeclaration(VariableDeclaration variableDeclaration)
        {
            if (_variableDeclarations == null) _variableDeclarations = new List<VariableDeclaration>();
            _variableDeclarations.Add(variableDeclaration);
        }

        public IEnumerable<VariableDeclaration> GetVariableDeclarations()
        {
            if (_variableDeclarations == null) return VariableDeclaration.EmptyArray;
            return _variableDeclarations;
        }

        public IVariableStore VariableStore => _variableStore;
    }
}
