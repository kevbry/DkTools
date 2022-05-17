using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Nodes
{
    class PropertyNode : Node, INamedNode
    {
        private string _name;
        private DataType _dataType;

        public PropertyNode(Node parent, string name, DataType dataType)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _dataType = dataType;
        }

        public string Name => _name;
        public DataType DataType => _dataType;
        public IEnumerable<PropertyAccessorNode> Getters => ChildNodes.Where(n => (n is PropertyAccessorNode a) && a.AccessorType == PropertyAccessorType.Getter).Cast<PropertyAccessorNode>();
        public IEnumerable<PropertyAccessorNode> Setters => ChildNodes.Where(n => (n is PropertyAccessorNode a) && a.AccessorType == PropertyAccessorType.Setter).Cast<PropertyAccessorNode>();

        public ObjectProperty ToObjectProperty()
        {
            var getters = Getters.Select(x => x.ToObjectPropertyAccessor()).ToArray();
            if (getters.Length == 0) getters = null;

            var setters = Setters.Select(x => x.ToObjectPropertyAccessor()).ToArray();
            if (setters.Length == 0) setters = null;

            return new ObjectProperty
            {
                Name = _name,
                DataType = _dataType.ToCode(),
                Getters = getters,
                Setters = setters
            };
        }
    }

    enum PropertyAccessorType
    {
        Getter,
        Setter
    }

    class PropertyAccessorNode : Node, IReturnTargetNode, IBodyNode
    {
        private PropertyAccessorType _accessorType;
        private Privacy _privacy;
        private FileContext _fileContext;
        private CodeSpan _bodySpan;

        public PropertyAccessorNode(PropertyNode property, PropertyAccessorType accessorType, Privacy privacy, FileContext fileContext, int bodyStartPos)
            : base(property)
        {
            _accessorType = accessorType;
            _privacy = privacy;
            _fileContext = fileContext;
            _bodySpan = new CodeSpan(bodyStartPos, bodyStartPos);

            if (accessorType == PropertyAccessorType.Setter)
            {
                // Add the implicit argument 'value'.
                AddVariable(new Variables.Variable(
                    name: "value",
                    dataType: property.DataType,
                    fileContext: fileContext,
                    passType: Variables.ArgumentPassType.ByValue,
                    initializer: null));
            }
        }

        public PropertyAccessorType AccessorType => _accessorType;

        public DataType ReturnDataType => _accessorType == PropertyAccessorType.Getter ? GetContainerOrNull<PropertyNode>().DataType : DataType.Void;

        public CodeSpan BodySpan { get => _bodySpan; set => _bodySpan = value; }

        public ObjectPropertyAccessor ToObjectPropertyAccessor() => new ObjectPropertyAccessor
        {
            Privacy = _privacy,
            FileContext = _fileContext,
            Body = GenerateObjectBody(_bodySpan.Start)
        };
    }
}
