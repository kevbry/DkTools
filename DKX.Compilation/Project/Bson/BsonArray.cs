using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonArray : BsonNode
    {
        private List<BsonNode> _elements = new List<BsonNode>();

        public BsonArray(BsonFile file) : base(file) { }

        public BsonArray(BsonFile file, IEnumerable<BsonNode> initialValues)
            : base(file)
        {
            foreach (var value in initialValues ?? throw new ArgumentNullException(nameof(initialValues))) Add(value);
        }

        public BsonArray(BsonFile file, BinaryReader bin)
            : base(file)
        {
            var numElements = bin.ReadInt32();
            for (var i = 0; i < numElements; i++)
            {
                _elements.Add(BsonNode.Read(file, bin));
            }
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_elements.Count);
            foreach (var element in _elements)
            {
                element.Write(bin);
            }
        }

        public int Length => _elements.Count;
        protected override NodeType NodeTypeId => NodeType.Array;
        public IEnumerable<BsonNode> Values => _elements;

        public BsonNode this[int index]
        {
            get
            {
                if (index < 0 || index >= _elements.Count) throw new ArgumentOutOfRangeException(nameof(index));
                return _elements[index];
            }
            set
            {
                if (index < 0 || index >= _elements.Count) throw new ArgumentOutOfRangeException(nameof(index));
                if (value.File != File) throw new ArgumentException("BSON node does not belong to the same file.");
                _elements[index] = value;
            }
        }

        public void Add(BsonNode value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.File != File) throw new ArgumentException("BSON node does not belong to the same file.");
            _elements.Add(value);
        }

        public void AddRange(IEnumerable<BsonNode> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            foreach (var value in values) Add(value);
        }

        public void Insert(int index, BsonNode value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.File != File) throw new ArgumentException("BSON node does not belong to the same file.");
            _elements.Insert(index, value);
        }

        public override void WriteJson(JsonWriter json)
        {
            json.WriteStartArray();
            foreach (var element in _elements) element.WriteJson(json);
            json.WriteEndArray();
        }
    }
}
