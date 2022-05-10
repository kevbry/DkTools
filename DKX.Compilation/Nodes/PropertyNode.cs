using DKX.Compilation.DataTypes;
using DKX.Compilation.Files;
using System;
using System.Linq;

namespace DKX.Compilation.Nodes
{
    class PropertyNode : Node, INamedNode
    {
        private string _name;
        private DataType _dataType;
        private Privacy _privacy;

        public PropertyNode(Node parent, string name, DataType dataType, Privacy privacy)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _dataType = dataType;
            _privacy = privacy;
        }

        public string Name => _name;
        public DataType DataType => _dataType;
        public PropertyAccessorNode Getter => ChildNodes.Where(n => (n is PropertyAccessorNode a) && a.AccessorType == PropertyAccessorType.Getter).Cast<PropertyAccessorNode>().FirstOrDefault();
        public PropertyAccessorNode Setter => ChildNodes.Where(n => (n is PropertyAccessorNode a) && a.AccessorType == PropertyAccessorType.Setter).Cast<PropertyAccessorNode>().FirstOrDefault();

        public ObjectProperty ToObjectProperty() => new ObjectProperty
        {
            Name = _name,
            Privacy = _privacy,
            DataType = _dataType.ToCode(),
            ReadOnly = Setter == null
        };
    }

    enum PropertyAccessorType
    {
        Getter,
        Setter
    }

    class PropertyAccessorNode : Node
    {
        private PropertyAccessorType _accessorType;

        public PropertyAccessorNode(PropertyNode property, PropertyAccessorType accessorType)
            : base(property)
        {
            _accessorType = accessorType;
        }

        public PropertyAccessorType AccessorType => _accessorType;
    }
}
