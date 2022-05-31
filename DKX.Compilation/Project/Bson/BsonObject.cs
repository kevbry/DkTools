using DKX.Compilation.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonObject : BsonNode
    {
        private Dictionary<int, BsonNode> _properties = new Dictionary<int, BsonNode>();

        public BsonObject(BsonFile file) : base(file) { }

        public BsonObject(BsonFile file, params object[] initialValues)
            : base(file)
        {
            if (initialValues.Length % 2 != 0) throw new ArgumentException("Initial values must be an even number of key-value pairs.");

            for (var i = 0; i < initialValues.Length; i += 2)
            {
                if (!(initialValues[i] is string)) throw new ArgumentException($"Initial values index {i} is not a string.");
                if (!(initialValues[i + 1] is BsonNode)) throw new ArgumentException($"Initial values index {i} is not a BsonNode.");
                this[(string)initialValues[i]] = (BsonNode)initialValues[i + 1];
            }
        }

        public BsonObject(BsonFile file, BinaryReader bin)
            : base(file)
        {
            var numProperties = (int)bin.ReadUInt16();
            for (var i = 0; i < numProperties; i++)
            {
                var nameId = bin.ReadInt32();
                var value = BsonNode.Read(file, bin);
                _properties[nameId] = value;
            }
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write((ushort)_properties.Count);
            foreach (var prop in _properties)
            {
                bin.Write(prop.Key);
                prop.Value.Write(bin);
            }
        }

        protected override NodeType NodeTypeId => NodeType.Object;

        public BsonNode this[string propertyName]
        {
            get
            {
                var id = File.GetStringId(propertyName ?? throw new ArgumentNullException(nameof(propertyName)));
                if (id < 0) throw new ArgumentOutOfRangeException(nameof(propertyName));
                return _properties[id];
            }
            set
            {
                if (value.File != File) throw new ArgumentException("BSON node does not belong to the same file.");
                var id = File.AddString(propertyName ?? throw new ArgumentNullException(nameof(propertyName)));
                _properties[id] = value ?? throw new ArgumentNullException();
            }
        }

        public bool HasProperty(string propertyName)
        {
            var id = File.GetStringId(propertyName ?? throw new ArgumentNullException(nameof(propertyName)));
            if (id < 0) return false;
            return _properties.ContainsKey(id);
        }

        public BsonNode GetProperty(string propertyName, bool throwIfMissing = true)
        {
            var id = File.GetStringId(propertyName ?? throw new ArgumentNullException(nameof(propertyName)));
            if (id < 0 || !_properties.TryGetValue(id, out var value))
            {
                if (throwIfMissing) throw new ArgumentOutOfRangeException(nameof(propertyName));
                return null;
            }
            return value;
        }

        public void AddProperty(string propertyName, BsonNode value)
        {
            var id = File.AddString(propertyName ?? throw new ArgumentNullException(nameof(propertyName)));
            if (_properties.ContainsKey(id)) throw new ArgumentException("Property already exists with the same name.");
            if (value.File != File) throw new ArgumentException("BSON node does not belong to the same file.");
            _properties[id] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void SetProperty(string propertyName, BsonNode value)
        {
            var id = File.AddString(propertyName ?? throw new ArgumentNullException(nameof(propertyName)));
            if (value.File != File) throw new ArgumentException("BSON node does not belong to the same file.");
            _properties[id] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public IEnumerable<BsonNode> GetArray(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonArray arr) return arr.Values;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return null;
        }

        public void SetArray(string propertyName, IEnumerable<BsonNode> nodes)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonArray(File, nodes);
        }

        public string GetString(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            return prop.ToString();
        }

        public void SetString(string propertyName, string value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonString(File, value ?? throw new ArgumentNullException(nameof(value)));
        }

        public DateTime GetDateTime(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonDateTime dt) return dt.Value;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetDateTime(string propertyName, DateTime value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonDateTime(File, value);
        }

        public DataType GetDataType(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonDataType dt) return dt.DataType;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetDataType(string propertyName, DataType value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonDataType(File, value);
        }

        public T GetEnum<T>(string propertyName, bool throwIfMissing = true) where T : Enum
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonEnum en) return en.GetValue<T>();
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetEnum(string propertyName, object value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonEnum(File, value);
        }

        public bool GetBoolean(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonBoolean b) return b.Value;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetBoolean(string propertyName, bool value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonBoolean(File, value);
        }

        public short GetInt16(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonInt16 b) return b.Value;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetInt16(string propertyName, short value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonInt16(File, value);
        }

        public ushort GetUInt16(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonUInt16 b) return b.Value;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetUInt16(string propertyName, ushort value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonUInt16(File, value);
        }

        public int GetInt32(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonInt32 b) return b.Value;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetInt32(string propertyName, int value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonInt32(File, value);
        }

        public uint GetUInt32(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonUInt32 b) return b.Value;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetUInt32(string propertyName, uint value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonUInt32(File, value);
        }

        public decimal GetDecimal(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonDecimal b) return b.Value;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetDecimal(string propertyName, decimal value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonDecimal(File, value);
        }

        public Span GetSpan(string propertyName, bool throwIfMissing = true)
        {
            var prop = GetProperty(propertyName, throwIfMissing);
            if (prop is BsonSpan b) return b.Value;
            if (throwIfMissing) throw new InvalidBsonTypeException();
            return default;
        }

        public void SetSpan(string propertyName, Span value)
        {
            this[propertyName ?? throw new ArgumentNullException(nameof(propertyName))] = new BsonSpan(File, value);
        }
    }
}
